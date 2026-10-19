using Microsoft.EntityFrameworkCore;
using OxBox.Application.Abstractions;
using OxBox.Domain.Alerts;
using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Settings;
using OxBox.Domain.Technicians;
using OxBox.Domain.Transactions;

namespace OxBox.Infrastructure.Persistence;

internal sealed class EfBoxStore(OxBoxDbContext db) : IBoxStore
{
    public async Task<IReadOnlyList<BoxEndpoint>> GetBoxEndpointsAsync(CancellationToken cancellationToken) =>
        await db.Boxes
            .AsNoTracking()
            .OrderBy(box => box.Code)
            .Select(box => new BoxEndpoint(box.Id, box.Code, box.BuHost, box.BuPort))
            .ToListAsync(cancellationToken);

    public Task<BoxEndpoint?> GetBoxEndpointAsync(int boxId, CancellationToken cancellationToken) =>
        db.Boxes
            .AsNoTracking()
            .Where(box => box.Id == boxId)
            .Select(box => new BoxEndpoint(box.Id, box.Code, box.BuHost, box.BuPort))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<BoxInventory> LoadInventoryAsync(int boxId, CancellationToken cancellationToken)
    {
        var box = await db.Boxes.Include(b => b.Positions).SingleAsync(b => b.Id == boxId, cancellationToken);

        var cylindersInBox = await db.Cylinders
            .Include(cylinder => cylinder.CostCentre)
            .Where(cylinder => cylinder.Location == CylinderLocation.InBox
                && db.Positions.Any(position => position.Id == cylinder.PositionId && position.BoxId == boxId))
            .ToListAsync(cancellationToken);

        return new BoxInventory(box, cylindersInBox);
    }

    public Task<Cylinder?> FindCylinderAsync(string barcode, CancellationToken cancellationToken) =>
        db.Cylinders.Include(cylinder => cylinder.CostCentre)
            .SingleOrDefaultAsync(cylinder => cylinder.Barcode == barcode.Trim(), cancellationToken);

    public Task<Technician?> FindActiveTechnicianAsync(string serviceBarcode, CancellationToken cancellationToken) =>
        db.Technicians.SingleOrDefaultAsync(technician => technician.ServiceBarcode == serviceBarcode.Trim() && technician.IsActive, cancellationToken);

    public Task<SystemSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        db.SystemSettings.SingleAsync(cancellationToken);

    public async Task<IReadOnlyList<CostCentre>> GetCostCentresServedByAsync(int boxId, CancellationToken cancellationToken)
    {
        var boxCode = await db.Boxes.Where(box => box.Id == boxId).Select(box => box.Code).SingleAsync(cancellationToken);
        return await db.CostCentres
            .Where(costCentre => costCentre.IsActive
                && db.Cylinders.Any(cylinder => cylinder.CostCentreId == costCentre.Id && cylinder.IsActive)
                && (db.CylinderTransactions.Any(t => t.BoxCode == boxCode && t.CostCentreCode == costCentre.Code)
                    || db.Cylinders.Any(cylinder => cylinder.CostCentreId == costCentre.Id
                        && db.Positions.Any(position => position.Id == cylinder.PositionId && position.BoxId == boxId))))
            .OrderBy(costCentre => costCentre.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Alert?> FindActiveAlertAsync(string key, CancellationToken cancellationToken) =>
        db.Alerts.SingleOrDefaultAsync(alert => alert.Key == key && alert.ClearedAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(int costCentreId, CancellationToken cancellationToken) =>
        await db.AlertRecipients
            .Where(recipient => recipient.IsActive && (recipient.CostCentreId == null || recipient.CostCentreId == costCentreId))
            .OrderBy(recipient => recipient.Name)
            .ToListAsync(cancellationToken);



    public void Add(CylinderStatusChange change) => db.CylinderStatusChanges.Add(change);

    public void Add(CylinderTransaction transaction) => db.CylinderTransactions.Add(transaction);

    public void Add(Alert alert) => db.Alerts.Add(alert);

    public void Add(OutboxMessage message) => db.OutboxMessages.Add(message);


    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
