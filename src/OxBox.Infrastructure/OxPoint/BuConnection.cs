using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.Infrastructure.OxPoint;

/// <summary>
/// WebSocket client for one OX Point BU. The BU is the WebSocket <i>server</i> (port 3030);
/// this class keeps a connection open, sends commands one at a time, matches replies by
/// content and forwards everything else as <see cref="GatewayEvent"/>s.
/// </summary>
public sealed partial class BuConnection : IBoxGateway
{
    // Documented BU messages are tiny; anything larger is a fault, not data.
    private const int MaxMessageBytes = 4 * 1024;

    // A command is sent again at most once, after the link dropped before its reply came.
    private const int MaxSendAttempts = 2;

    // How often a waiting command looks again when the socket has closed but the receive loop
    // has not yet reported the drop.
    private static readonly TimeSpan ClosingPollInterval = TimeSpan.FromMilliseconds(50);

    private readonly Uri _uri;
    private readonly BuConnectionOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BuConnection> _logger;
    private readonly Channel<GatewayEvent> _events =
        Channel.CreateUnbounded<GatewayEvent>(new UnboundedChannelOptions { SingleReader = true });

    // Only one command is in flight at a time, so a reply can be matched to it by content.
    private readonly SemaphoreSlim _sendGate = new(1, 1);

    private ClientWebSocket? _socket;
    private PendingCommand? _pending;

    // Completes when the link is up; replaced by a fresh one when it drops.
    private TaskCompletionSource _connected = NewConnectedSignal();

    public BuConnection(Uri uri, BuConnectionOptions options, TimeProvider timeProvider, ILogger<BuConnection> logger)
    {
        _uri = uri;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ChannelReader<GatewayEvent> Events => _events.Reader;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var reconnectDelay = _options.InitialReconnectDelay;

        while (!cancellationToken.IsCancellationRequested)
        {
            var wasConnected = await ConnectAndReceiveAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            reconnectDelay = wasConnected ? _options.InitialReconnectDelay : NextDelay(reconnectDelay);
            LogReconnecting(_uri, reconnectDelay);

            try
            {
                await Task.Delay(reconnectDelay, _timeProvider, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _events.Writer.TryComplete();
    }

    /// <remarks>
    /// The real BU (firmware 13.IO2.01) drops its link every 20–30 s and is back within seconds,
    /// so a command waits up to <see cref="BuConnectionOptions.ReconnectWait"/> for the link
    /// instead of failing, and is sent once more if the link drops before the reply. Opening a
    /// lock twice is harmless: the second open finds it already unlatched.
    /// </remarks>
    public async Task<CommandResult> SendAsync(BuCommand command, CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken);
        try
        {
            var deadline = _timeProvider.GetUtcNow() + _options.ReconnectWait;
            for (var attempt = 1; ; attempt++)
            {
                var socket = await WaitForLinkAsync(deadline, cancellationToken);
                if (socket is null)
                {
                    return CommandResult.NotConnected;
                }

                var result = await SendOnceAsync(socket, command, cancellationToken);
                if (result.Outcome != CommandOutcome.NotConnected || attempt == MaxSendAttempts)
                {
                    return result;
                }

                LogResending(_uri, command);
            }
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _sendGate.Dispose();
        _events.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }

    private static TaskCompletionSource NewConnectedSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Returns the open socket, or <c>null</c> if the link is not up by <paramref name="deadline"/>.</summary>
    private async Task<ClientWebSocket?> WaitForLinkAsync(DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        while (true)
        {
            // Read the signal before the socket: a drop replaces the signal before clearing the socket.
            var connected = Volatile.Read(ref _connected);
            var socket = Volatile.Read(ref _socket);
            if (socket is { State: WebSocketState.Open })
            {
                return socket;
            }

            var remaining = deadline - _timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
            {
                return null;
            }

            if (connected.Task.IsCompleted)
            {
                await Task.Delay(ClosingPollInterval < remaining ? ClosingPollInterval : remaining, _timeProvider, cancellationToken);
                continue;
            }

            try
            {
                await connected.Task.WaitAsync(remaining, _timeProvider, cancellationToken);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }
    }

    private async Task<CommandResult> SendOnceAsync(ClientWebSocket socket, BuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var pending = command.ExpectsReply ? new PendingCommand(command) : null;
            Volatile.Write(ref _pending, pending);

            var json = command.ToJson();
            LogSending(_uri, json);
            await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);

            return pending is null
                ? CommandResult.SentWithoutReply
                : await WaitForReplyAsync(pending, cancellationToken);
        }
        catch (WebSocketException exception)
        {
            LogSendFailed(exception, _uri, command);
            return CommandResult.NotConnected;
        }
        finally
        {
            Volatile.Write(ref _pending, null);
        }
    }

    private async Task<CommandResult> WaitForReplyAsync(PendingCommand pending, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await pending.Reply.WaitAsync(_options.CommandTimeout, _timeProvider, cancellationToken);
            return reply is null ? CommandResult.NotConnected : CommandResult.Replied(reply);
        }
        catch (TimeoutException)
        {
            LogCommandTimedOut(_uri, pending.Command);
            return CommandResult.TimedOut;
        }
    }

    /// <summary>Returns whether a connection was established before it ended.</summary>
    private async Task<bool> ConnectAndReceiveAsync(CancellationToken cancellationToken)
    {
        using var socket = CreateSocket();
        var connected = false;
        try
        {
            using (var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectTimeout.CancelAfter(_options.ConnectTimeout);
                await socket.ConnectAsync(_uri, connectTimeout.Token);
            }

            connected = true;
            Volatile.Write(ref _socket, socket);
            Volatile.Read(ref _connected).TrySetResult();
            LogConnected(_uri);
            _events.Writer.TryWrite(new ConnectionChanged(IsConnected: true));

            await ReceiveUntilClosedAsync(socket, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
        {
            // Connect timeout, refused connection, dropped link or failed keep-alive.
            LogConnectionLost(exception, _uri);
        }
        finally
        {
            if (connected)
            {
                Volatile.Write(ref _connected, NewConnectedSignal());
                Volatile.Write(ref _socket, null);
                Volatile.Read(ref _pending)?.Abandon();
                _events.Writer.TryWrite(new ConnectionChanged(IsConnected: false));
            }
        }

        return connected;
    }

    private ClientWebSocket CreateSocket()
    {
        var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = _options.KeepAliveInterval;
        socket.Options.KeepAliveTimeout = _options.KeepAliveTimeout > TimeSpan.Zero
            ? _options.KeepAliveTimeout
            : Timeout.InfiniteTimeSpan;
        return socket;
    }

    private async Task ReceiveUntilClosedAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxMessageBytes];
        var length = 0;
        var oversized = false;

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer.AsMemory(length), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            length += result.Count;
            oversized |= length == buffer.Length && !result.EndOfMessage;
            if (oversized)
            {
                // Drop the frame but keep reading until its end, so the next message starts clean.
                length = 0;
            }

            if (!result.EndOfMessage)
            {
                continue;
            }

            if (oversized)
            {
                LogOversizedMessage(_uri, MaxMessageBytes);
            }
            else
            {
                Dispatch(Encoding.UTF8.GetString(buffer, 0, length));
            }

            length = 0;
            oversized = false;
        }
    }

    private void Dispatch(string json)
    {
        var message = BuMessageParser.Parse(json);
        LogReceived(_uri, json);

        if (message is UnrecognizedMessage)
        {
            LogUnrecognized(_uri, json);
        }

        var pending = Volatile.Read(ref _pending);
        if (pending is not null && pending.Command.IsAnsweredBy(message) && pending.TryComplete(message))
        {
            return;
        }

        // Unsolicited message, or a reply that arrived after its command timed out.
        _events.Writer.TryWrite(new BuMessageReceived(message));
    }

    private TimeSpan NextDelay(TimeSpan current) =>
        TimeSpan.FromTicks(Math.Min(current.Ticks * 2, _options.MaxReconnectDelay.Ticks));

    private sealed class PendingCommand(BuCommand command)
    {
        private readonly TaskCompletionSource<BuMessage?> _reply =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BuCommand Command { get; } = command;

        /// <summary>Completes with the reply, or with <c>null</c> if the connection dropped.</summary>
        public Task<BuMessage?> Reply => _reply.Task;

        public bool TryComplete(BuMessage reply) => _reply.TrySetResult(reply);

        public void Abandon() => _reply.TrySetResult(null);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to BU {Uri}")]
    private partial void LogConnected(Uri uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection to BU {Uri} lost or failed")]
    private partial void LogConnectionLost(Exception exception, Uri uri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reconnecting to BU {Uri} in {Delay}")]
    private partial void LogReconnecting(Uri uri, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Debug, Message = "BU {Uri} <- {Json}")]
    private partial void LogSending(Uri uri, string json);

    [LoggerMessage(Level = LogLevel.Debug, Message = "BU {Uri} -> {Json}")]
    private partial void LogReceived(Uri uri, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "Link to BU {Uri} dropped before the reply to {Command}; sending it again")]
    private partial void LogResending(Uri uri, BuCommand command);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending {Command} to BU {Uri} failed")]
    private partial void LogSendFailed(Exception exception, Uri uri, BuCommand command);

    [LoggerMessage(Level = LogLevel.Warning, Message = "BU {Uri} did not reply to {Command} in time")]
    private partial void LogCommandTimedOut(Uri uri, BuCommand command);

    [LoggerMessage(Level = LogLevel.Warning, Message = "BU {Uri} sent an unrecognized message: {Json}")]
    private partial void LogUnrecognized(Uri uri, string json);

    [LoggerMessage(Level = LogLevel.Warning, Message = "BU {Uri} sent a message over {MaxBytes} bytes; dropped")]
    private partial void LogOversizedMessage(Uri uri, int maxBytes);
}
