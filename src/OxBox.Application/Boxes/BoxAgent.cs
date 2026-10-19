using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OxBox.Application.Abstractions;
using OxBox.Application.Feedback;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.Application.Boxes;

/// <summary>
/// The single owner of one box. Every event for the box (scans, connection changes and ticks)
/// goes through one queue and is handled one at a time, so the workflows never race each other.
/// </summary>
/// <remarks>Only one server may run the agents: nothing stops a second one from driving the same box.</remarks>
public sealed partial class BoxAgent(
    BoxEndpoint box,
    IBoxGatewayFactory gatewayFactory,
    IKioskDisplay display,
    BoxStatusBoard statusBoard,
    IServiceScopeFactory scopeFactory,
    IOptions<BoxOptions> options,
    TimeProvider time,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<BoxAgent>();

    public int BoxId => box.BoxId;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        display.Show(box.BoxCode, new OutOfServiceScreen());
        statusBoard.Report(box.BoxId, isRunHere: true, isConnected: false);

        await using var gateway = gatewayFactory.Create(box.Host, box.Port);
        var hardware = new BoxHardware(box.BoxCode, gateway, options.Value.OpenAttempts, loggerFactory.CreateLogger<BoxHardware>());
        var signals = new BoxSignals(box.BoxCode, hardware, display, loggerFactory.CreateLogger<BoxSignals>());
        var link = new BoxLink(box.BoxId, box.BoxCode, hardware, signals);
        var events = Channel.CreateUnbounded<BoxEvent>(new UnboundedChannelOptions { SingleReader = true });

        var producers = Task.WhenAll(
            gateway.RunAsync(cancellationToken),
            ForwardGatewayEventsAsync(gateway, events.Writer, cancellationToken),
            ProduceTicksAsync(events.Writer, cancellationToken));

        try
        {
            await foreach (var boxEvent in events.Reader.ReadAllAsync(cancellationToken))
            {
                await HandleSafelyAsync(link, boxEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down.
        }

        await producers;
        statusBoard.Report(box.BoxId, isRunHere: false, isConnected: false);
    }

    private static async Task ForwardGatewayEventsAsync(IBoxGateway gateway, ChannelWriter<BoxEvent> events, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var gatewayEvent in gateway.Events.ReadAllAsync(cancellationToken))
            {
                await events.WriteAsync(new FromGateway(gatewayEvent), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task ProduceTicksAsync(ChannelWriter<BoxEvent> events, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(options.Value.TickInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await events.WriteAsync(new Tick(), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    // One failing event must not stop the box: log it and carry on with the next one.
    private async Task HandleSafelyAsync(BoxLink link, BoxEvent boxEvent, CancellationToken cancellationToken)
    {
        try
        {
            await HandleAsync(link, boxEvent, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogEventFailed(exception, box.BoxCode, boxEvent);
        }
    }

    private Task HandleAsync(BoxLink link, BoxEvent boxEvent, CancellationToken cancellationToken) => boxEvent switch
    {
        FromGateway { Event: ConnectionChanged { IsConnected: true } } => OnConnectedAsync(link, cancellationToken),
        FromGateway { Event: ConnectionChanged { IsConnected: false } } => OnDisconnected(),
        FromGateway { Event: BuMessageReceived { Message: ScanReceived scan } } =>
            WithEventsAsync(events => events.ScannedAsync(link, scan.Code, cancellationToken)),
        FromGateway { Event: BuMessageReceived { Message: BuNotification notification } } => OnNotification(notification),
        Tick when link.Visit is not null => WithEventsAsync(events => events.TickAsync(link, cancellationToken)),
        _ => Task.CompletedTask,
    };

    private Task OnConnectedAsync(BoxLink link, CancellationToken cancellationToken)
    {
        statusBoard.Report(box.BoxId, isRunHere: true, isConnected: true);
        LogConnected(box.BoxCode);
        return WithEventsAsync(events => events.ConnectedAsync(link, cancellationToken));
    }

    private Task OnDisconnected()
    {
        statusBoard.Report(box.BoxId, isRunHere: true, isConnected: false);
        LogDisconnected(box.BoxCode);
        display.Show(box.BoxCode, new OutOfServiceScreen());
        return Task.CompletedTask;
    }

    // Box door (TAMPER), OPEN ALL and similar notifications are not part of the RFQ; they are only logged.
    private Task OnNotification(BuNotification notification)
    {
        LogNotification(box.BoxCode, notification.Kind);
        return Task.CompletedTask;
    }

    private async Task WithEventsAsync(Func<BoxEvents, Task> handle)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await handle(scope.ServiceProvider.GetRequiredService<BoxEvents>());
    }

    private abstract record BoxEvent;

    private sealed record FromGateway(GatewayEvent Event) : BoxEvent;

    private sealed record Tick : BoxEvent;

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: connected to its BU")]
    private partial void LogConnected(string boxCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Box {BoxCode}: BU connection lost")]
    private partial void LogDisconnected(string boxCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Box {BoxCode}: BU reported {Notification}")]
    private partial void LogNotification(string boxCode, BuNotificationKind notification);

    [LoggerMessage(Level = LogLevel.Error, Message = "Box {BoxCode}: handling {Event} failed")]
    private partial void LogEventFailed(Exception exception, string boxCode, object @event);
}
