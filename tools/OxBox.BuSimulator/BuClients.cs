using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.BuSimulator;

/// <summary>
/// The BU's WebSocket server side: accepts any number of clients, answers each client's
/// commands to that client only, and broadcasts notifications to all clients, like the real BU.
/// </summary>
public sealed partial class BuClients(SimulatedBu bu, TimeProvider time, ILogger<BuClients> logger)
{
    // The document says the BU reports a WebSocket outage of 15 s or more once a client returns.
    private static readonly TimeSpan ReportableOutage = TimeSpan.FromSeconds(15);

    private readonly ConcurrentDictionary<Guid, Client> _clients = new();
    private DateTimeOffset? _lastClientLeftAt;

    public int Count => _clients.Count;

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var client = new Client(socket);
        var wasOutage = _clients.IsEmpty && _lastClientLeftAt is { } leftAt && time.GetUtcNow() - leftAt >= ReportableOutage;
        _clients[client.Id] = client;
        LogClientConnected(context.Connection.RemoteIpAddress?.ToString(), _clients.Count);

        try
        {
            if (wasOutage)
            {
                await BroadcastAsync(new BuNotification(BuNotificationKind.WebSocketWasDisconnected));
            }

            await ServeAsync(client, context.RequestAborted);
        }
        finally
        {
            _clients.TryRemove(client.Id, out _);
            if (_clients.IsEmpty)
            {
                _lastClientLeftAt = time.GetUtcNow();
            }

            LogClientDisconnected(_clients.Count);
        }
    }

    public async Task BroadcastAsync(BuMessage message)
    {
        foreach (var client in _clients.Values)
        {
            await SendAsync(client, message);
        }
    }

    private async Task ServeAsync(Client client, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (client.Socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await client.Socket.ReceiveAsync(buffer, cancellationToken);
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
            {
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            bu.RecordTraffic(TrafficDirection.In, json);

            var command = BuCommandParser.Parse(json);
            if (command is not null && bu.Handle(command) is { } reply)
            {
                await SendAsync(client, reply);
            }
        }
    }

    private async Task SendAsync(Client client, BuMessage message)
    {
        var json = BuMessageWriter.ToJson(message);
        bu.RecordTraffic(TrafficDirection.Out, json);

        // A WebSocket allows only one send at a time; replies and broadcasts can overlap.
        await client.SendGate.WaitAsync();
        try
        {
            await client.Socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
        catch (WebSocketException)
        {
            // The client is going away; its receive loop will clean up.
        }
        finally
        {
            client.SendGate.Release();
        }
    }

    private sealed class Client(WebSocket socket)
    {
        public Guid Id { get; } = Guid.NewGuid();

        public WebSocket Socket { get; } = socket;

        public SemaphoreSlim SendGate { get; } = new(1, 1);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Client {RemoteAddress} connected ({Count} connected)")]
    private partial void LogClientConnected(string? remoteAddress, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client disconnected ({Count} connected)")]
    private partial void LogClientDisconnected(int count);
}
