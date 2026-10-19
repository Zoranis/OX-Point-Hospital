using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;
using OxBox.Application.Feedback;
using OxBox.Application.Inventory;

namespace OxBox.Application.Exchange;

/// <summary>
/// Runs the empty-for-full exchange (RFQ "Process for Returning an Empty Cylinder and Dispensing
/// a Full One"): check the scan, unlock a free slot for the empty cylinder and a full cylinder's
/// slot of the same cost centre, and record both movements.
/// </summary>
/// <remarks>
/// Each movement is recorded as soon as its lock confirms it opened; doors are not watched.
/// The return is saved before the full cylinder's slot is unlocked, so a failure there still
/// leaves an accurate record of the empty cylinder taken back.
/// </remarks>
public sealed partial class ExchangeWorkflow(IBoxStore store, CylinderLedger ledger, ILogger<ExchangeWorkflow> logger)
{
    /// <summary>Handles a cylinder barcode scanned at the box.</summary>
    public async Task ScanAsync(BoxLink box, string barcode, CancellationToken cancellationToken)
    {
        var inventory = await store.LoadInventoryAsync(box.BoxId, cancellationToken);
        var scanned = await store.FindCylinderAsync(barcode, cancellationToken);

        var decision = ExchangeRules.Decide(scanned, inventory);
        if (decision is ExchangeRejected rejected)
        {
            LogRejected(box.BoxCode, barcode, rejected.Error);
            await box.Signals.RejectAsync(rejected.Error, returnPositionNumber: null, cancellationToken);
            return;
        }

        var plan = (ExchangeApproved)decision;
        if (await box.Hardware.OpenAsync(plan.ReturnPosition, cancellationToken) != OpenOutcome.Opened)
        {
            LogLockFailed(box.BoxCode, plan.ReturnPosition.Number, barcode);
            await box.Signals.RejectAsync(ExchangeError.HardwareFault, returnPositionNumber: null, cancellationToken);
            return;
        }

        await ledger.ReturnedAsync(inventory.Box, plan.ReturnPosition, plan.ReturnCylinder, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        if (await box.Hardware.OpenAsync(plan.DispensePosition, cancellationToken) != OpenOutcome.Opened)
        {
            LogDispenseFailed(box.BoxCode, plan.DispensePosition.Number, barcode, plan.ReturnCylinder.CostCentre.Code);
            await box.Signals.RejectAsync(ExchangeError.DispenseFailed, plan.ReturnPosition.Number, cancellationToken);
            return;
        }

        await ledger.DispensedAsync(inventory.Box, plan.DispensePosition, plan.DispenseCylinder, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        LogCompleted(box.BoxCode, barcode, plan.ReturnPosition.Number, plan.DispenseCylinder.Barcode, plan.DispensePosition.Number);

        await box.Signals.SucceedAsync(new ExchangeOpenScreen(plan.ReturnPosition.Number, plan.DispensePosition.Number), cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: scan {Barcode} rejected: {Error}")]
    private partial void LogRejected(string boxCode, string barcode, ExchangeError error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: {Barcode} returned to position {ReturnPosition}, {Dispensed} dispensed from position {DispensePosition}")]
    private partial void LogCompleted(string boxCode, string barcode, int returnPosition, string dispensed, int dispensePosition);

    [LoggerMessage(Level = LogLevel.Error, Message = "Box {BoxCode}: position {Position} did not open for the return of {Barcode}; nothing recorded")]
    private partial void LogLockFailed(string boxCode, int position, string barcode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Box {BoxCode}: position {Position} did not open; {Barcode} was returned but no full cylinder was dispensed, cost centre {CostCentre} is owed one")]
    private partial void LogDispenseFailed(string boxCode, int position, string barcode, string costCentre);
}
