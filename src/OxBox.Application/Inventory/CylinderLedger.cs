using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Domain.Boxes;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Transactions;

namespace OxBox.Application.Inventory;

/// <summary>
/// Records cylinder movements at a box. Every movement changes the cylinder's state through the
/// domain (which produces the audit record) and adds a transaction carrying the RFQ's full/empty
/// flag in force at that moment. Changes are saved with the caller's unit of work.
/// </summary>
public sealed partial class CylinderLedger(IBoxStore store, TimeProvider time, ILogger<CylinderLedger> logger)
{
    /// <summary>A user put this empty cylinder into <paramref name="position"/>.</summary>
    public async Task ReturnedAsync(Box box, Position position, Cylinder cylinder, CancellationToken cancellationToken)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        store.Add(cylinder.ReturnToBox(position, time.GetUtcNow()));
        store.Add(Transaction(CylinderAction.Return, box, position.Number, cylinder, settings.EmptyFlag));
        LogMovement(box.Code, CylinderAction.Return, cylinder.Barcode, position.Number);
    }

    /// <summary>A user took this full cylinder out of <paramref name="position"/>. This is what is invoiced.</summary>
    public async Task DispensedAsync(Box box, Position position, Cylinder cylinder, CancellationToken cancellationToken)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        store.Add(cylinder.DispenseToCostCentre(time.GetUtcNow()));
        store.Add(Transaction(CylinderAction.Dispense, box, position.Number, cylinder, settings.FullFlag));
        LogMovement(box.Code, CylinderAction.Dispense, cylinder.Barcode, position.Number);
    }

    /// <summary>A technician took this empty cylinder out of <paramref name="position"/> for refilling.</summary>
    public async Task SentToSupplierAsync(Box box, Position position, Cylinder cylinder, int technicianId, CancellationToken cancellationToken)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        store.Add(cylinder.SendToSupplier(time.GetUtcNow()));
        store.Add(Transaction(CylinderAction.RestockOut, box, position.Number, cylinder, settings.EmptyFlag, technicianId));
        LogMovement(box.Code, CylinderAction.RestockOut, cylinder.Barcode, position.Number);
    }

    /// <summary>A technician put this refilled cylinder into <paramref name="position"/>; it is full from now on.</summary>
    public async Task RestockedAsync(Box box, Position position, Cylinder cylinder, int technicianId, CancellationToken cancellationToken)
    {
        var settings = await store.GetSettingsAsync(cancellationToken);
        store.Add(cylinder.RestockIntoBox(position, time.GetUtcNow()));
        store.Add(Transaction(CylinderAction.RestockIn, box, position.Number, cylinder, settings.FullFlag, technicianId));
        LogMovement(box.Code, CylinderAction.RestockIn, cylinder.Barcode, position.Number);
    }

    private CylinderTransaction Transaction(CylinderAction action, Box box, int positionNumber, Cylinder cylinder, int flag, int? technicianId = null) =>
        new(time.GetUtcNow(), action, box.Code, positionNumber, cylinder.Barcode, cylinder.CostCentre.Code, flag, technicianId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Box {BoxCode}: {Action} of {Barcode} at position {Position}")]
    private partial void LogMovement(string boxCode, CylinderAction action, string barcode, int position);
}
