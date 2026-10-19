using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;
using OxBox.Application.Feedback;
using OxBox.Application.Inventory;
using OxBox.Domain.Technicians;

namespace OxBox.Application.Service;

/// <summary>
/// Runs a technician's restocking visit (RFQ "Service Restocking"): the service code unlocks every
/// slot holding an empty cylinder, each full cylinder scanned gets a free slot, and the visit ends
/// with the service code again or after a period without scans.
/// </summary>
/// <remarks>As in the exchange, each movement is recorded as soon as its lock confirms it opened.</remarks>
public sealed partial class ServiceWorkflow(
    IBoxStore store,
    CylinderLedger ledger,
    IOptions<ServiceOptions> options,
    TimeProvider time,
    ILogger<ServiceWorkflow> logger)
{
    /// <summary>A technician scanned their service code at a box without an open visit.</summary>
    public async Task StartAsync(BoxLink box, Technician technician, CancellationToken cancellationToken)
    {
        var inventory = await store.LoadInventoryAsync(box.BoxId, cancellationToken);
        var opened = new List<int>();
        foreach (var position in inventory.PositionsWithEmptyCylinders())
        {
            if (await box.Hardware.OpenAsync(position, cancellationToken) != OpenOutcome.Opened)
            {
                // Its empty stays recorded in the box and is collected on a later visit.
                LogEmptySlotFailed(box.BoxCode, position.Number);
                continue;
            }

            await ledger.SentToSupplierAsync(inventory.Box, position, inventory.CylinderAt(position)!, technician.Id, cancellationToken);
            opened.Add(position.Number);
        }

        await store.SaveChangesAsync(cancellationToken);
        box.Visit = new ServiceVisit(technician.Id, technician.Name, opened, time.GetUtcNow());
        LogStarted(box.BoxCode, technician.Name, opened.Count);
        await box.Signals.SucceedAsync(ScreenFor(box.Visit), cancellationToken);
    }

    /// <summary>A cylinder barcode was scanned during the visit: a refilled cylinder to put in.</summary>
    public async Task InsertAsync(BoxLink box, string barcode, CancellationToken cancellationToken)
    {
        var visit = box.Visit!;
        visit.NoteActivity(time.GetUtcNow());

        var inventory = await store.LoadInventoryAsync(box.BoxId, cancellationToken);
        var scanned = await store.FindCylinderAsync(barcode, cancellationToken);
        var decision = ServiceRules.DecideInsert(scanned, inventory);
        if (decision is InsertRejected rejected)
        {
            LogInsertRejected(box.BoxCode, barcode, rejected.Error);
            await box.Signals.RejectServiceAsync(rejected.Error, ScreenFor(visit), cancellationToken);
            return;
        }

        var approved = (InsertApproved)decision;
        if (await box.Hardware.OpenAsync(approved.Position, cancellationToken) != OpenOutcome.Opened)
        {
            LogInsertSlotFailed(box.BoxCode, approved.Position.Number, barcode);
            await box.Signals.RejectServiceAsync(ServiceError.NoSlotOpened, ScreenFor(visit), cancellationToken);
            return;
        }

        await ledger.RestockedAsync(inventory.Box, approved.Position, approved.Cylinder, visit.TechnicianId, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        visit.Restocked(approved.Position.Number);
        await box.Signals.SucceedAsync(ScreenFor(visit), cancellationToken);
    }

    /// <summary>A service code was scanned again: the visit ends.</summary>
    public async Task FinishAsync(BoxLink box, CancellationToken cancellationToken)
    {
        var visit = End(box, "service code");
        await box.Signals.SucceedAsync(new ServiceFinishedScreen(visit.EmptySlots.Count, visit.RestockedCount), cancellationToken);
    }

    /// <summary>Ends the visit if nothing has been scanned for a while.</summary>
    /// <returns>True if the visit timed out now.</returns>
    public bool EndIfInactive(BoxLink box)
    {
        if (box.Visit is not { } visit || !visit.IsInactive(time.GetUtcNow(), options.Value.InactivityTimeout))
        {
            return false;
        }

        End(box, "timeout");
        box.Signals.Show(new ServiceFinishedScreen(visit.EmptySlots.Count, visit.RestockedCount));
        return true;
    }

    public static ServiceScreen ScreenFor(ServiceVisit visit) =>
        new(visit.TechnicianName, visit.EmptySlots, visit.RestockedCount, visit.LastInsertedPositionNumber);

    private ServiceVisit End(BoxLink box, string reason)
    {
        var visit = box.Visit!;
        box.Visit = null;
        LogEnded(box.BoxCode, visit.TechnicianName, reason, visit.EmptySlots.Count, visit.RestockedCount);
        return visit;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: service visit started by {Technician}; {EmptyCount} slot(s) with empty cylinders unlocked")]
    private partial void LogStarted(string boxCode, string technician, int emptyCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: service scan {Barcode} rejected: {Error}")]
    private partial void LogInsertRejected(string boxCode, string barcode, ServiceError error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Box {BoxCode}: position {Position} did not open to take its empty cylinder out; it stays recorded in the box")]
    private partial void LogEmptySlotFailed(string boxCode, int position);

    [LoggerMessage(Level = LogLevel.Error, Message = "Box {BoxCode}: position {Position} did not open for {Barcode}; nothing recorded")]
    private partial void LogInsertSlotFailed(string boxCode, int position, string barcode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: service visit by {Technician} ended ({Reason}): {Removed} out, {Restocked} in")]
    private partial void LogEnded(string boxCode, string technician, string reason, int removed, int restocked);
}
