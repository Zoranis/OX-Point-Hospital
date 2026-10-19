using OxBox.Application.Abstractions;
using OxBox.Application.Alerts;
using OxBox.Application.Exchange;
using OxBox.Application.Feedback;
using OxBox.Application.Service;

namespace OxBox.Application.Boxes;

/// <summary>
/// Everything that can happen at a box, routed to the workflow that handles it. One instance
/// serves one event, in its own unit of work; the box's agent calls it one event at a time.
/// </summary>
/// <remarks>
/// After each event the stock alerts are brought up to date, except during a service visit: the
/// stock is being changed on purpose, and a "low stock" e-mail halfway through restocking would be
/// noise. The visit's end is evaluated like any other event.
/// </remarks>
public sealed class BoxEvents(
    IBoxStore store,
    ExchangeWorkflow exchange,
    ServiceWorkflow service,
    StockAlertEvaluator stockAlerts)
{
    /// <summary>The BU connection came up: show the right screen.</summary>
    public async Task ConnectedAsync(BoxLink box, CancellationToken cancellationToken)
    {
        box.Signals.Show(CurrentScreen(box));
        await FinishAsync(box, cancellationToken);
    }

    public async Task ScannedAsync(BoxLink box, string code, CancellationToken cancellationToken)
    {
        var technician = await store.FindActiveTechnicianAsync(code, cancellationToken);

        var task = (box.Visit, technician) switch
        {
            ({ }, { }) => service.FinishAsync(box, cancellationToken),
            ({ }, null) => service.InsertAsync(box, code, cancellationToken),
            (null, { }) => service.StartAsync(box, technician, cancellationToken),
            _ => exchange.ScanAsync(box, code, cancellationToken),
        };
        await task;

        await FinishAsync(box, cancellationToken);
    }

    /// <summary>The periodic check for a service visit left without scans.</summary>
    public async Task TickAsync(BoxLink box, CancellationToken cancellationToken)
    {
        if (service.EndIfInactive(box))
        {
            await FinishAsync(box, cancellationToken);
        }
    }

    /// <summary>What the box display should show right now.</summary>
    public static KioskScreen CurrentScreen(BoxLink box) =>
        box.Visit is { } visit ? ServiceWorkflow.ScreenFor(visit) : new ReadyScreen();

    private async Task FinishAsync(BoxLink box, CancellationToken cancellationToken)
    {
        if (box.Visit is null)
        {
            await stockAlerts.EvaluateAsync(box.BoxId, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);
        }
    }
}
