using OxBox.Domain.Alerts;
using OxBox.Domain.Boxes;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Settings;
using OxBox.Domain.Technicians;
using OxBox.Domain.Transactions;

namespace OxBox.Application.Abstractions;

/// <summary>
/// Data access for box workflows, as one unit of work: entities loaded through a store are
/// tracked, and <see cref="SaveChangesAsync"/> writes every change atomically.
/// </summary>
public interface IBoxStore
{
    Task<IReadOnlyList<BoxEndpoint>> GetBoxEndpointsAsync(CancellationToken cancellationToken);

    Task<BoxEndpoint?> GetBoxEndpointAsync(int boxId, CancellationToken cancellationToken);

    /// <summary>The box with its positions and the cylinders in it (with cost centres).</summary>
    Task<BoxInventory> LoadInventoryAsync(int boxId, CancellationToken cancellationToken);

    Task<Cylinder?> FindCylinderAsync(string barcode, CancellationToken cancellationToken);

    /// <summary>The active technician with this service code, or <c>null</c>.</summary>
    Task<Technician?> FindActiveTechnicianAsync(string serviceBarcode, CancellationToken cancellationToken);

    Task<SystemSettings> GetSettingsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Active cost centres that use this box: they have active cylinders, and at least one of them
    /// is in the box or has moved through it. Stock alerts are checked for these only.
    /// </summary>
    Task<IReadOnlyList<CostCentre>> GetCostCentresServedByAsync(int boxId, CancellationToken cancellationToken);

    Task<Alert?> FindActiveAlertAsync(string key, CancellationToken cancellationToken);

    /// <summary>Active recipients of every alert, plus those of the given cost centre.</summary>
    Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(int costCentreId, CancellationToken cancellationToken);

    void Add(CylinderStatusChange change);

    void Add(CylinderTransaction transaction);

    void Add(Alert alert);

    void Add(OutboxMessage message);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
