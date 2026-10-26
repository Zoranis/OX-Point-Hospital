using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OxBox.Integration.Tests.OxPoint;

/// <summary>
/// An in-process stand-in for an OX Point BU: a WebSocket server that records the commands it
/// receives, answers them through <see cref="Respond"/> and can push unsolicited messages.
/// </summary>
internal sealed class FakeBu : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<string> _received = new();
    private readonly TaskCompletionSource _clientConnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private WebSocket? _client;
    private int _dropNextCommand;
    private bool _disposed;

    private FakeBu(WebApplication app) => _app = app;

    /// <summary>Returns the reply to send for a received command, or <c>null</c> to stay silent.</summary>
    public Func<string, string?> Respond { get; set; } = _ => null;

    public IReadOnlyCollection<string> ReceivedCommands => _received;

    public Uri Uri { get; private set; } = null!;

    public static async Task<FakeBu> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        var fake = new FakeBu(app);
        app.UseWebSockets();
        app.Run(fake.HandleAsync);

        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        fake.Uri = new Uri(address.Replace("http://", "ws://", StringComparison.Ordinal));
        return fake;
    }

    public Task WaitForClientAsync() => _clientConnected.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>Drops the link when the next command arrives, before answering it, as the real BU does now and then.</summary>
    public void DropLinkOnNextCommand() => Volatile.Write(ref _dropNextCommand, 1);

    public Task PushAsync(string json) =>
        _client!.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Abort the open socket first; otherwise StopAsync waits for the request to finish.
        _client?.Abort();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        _client = socket;
        _clientConnected.TrySetResult();

        var buffer = new byte[4096];
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(buffer, context.RequestAborted);
            }
            catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
            {
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            var command = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _received.Enqueue(command);

            if (Interlocked.Exchange(ref _dropNextCommand, 0) == 1)
            {
                socket.Abort();
                return;
            }

            if (Respond(command) is { } reply)
            {
                await socket.SendAsync(Encoding.UTF8.GetBytes(reply), WebSocketMessageType.Text, endOfMessage: true, context.RequestAborted);
            }
        }
    }
}
