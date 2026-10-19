using OxBox.Application.Abstractions;
using OxBox.Domain.Alerts;

namespace OxBox.Application.Alerts;

/// <summary>
/// Keeps a box's stock alerts in line with what is in the box. It compares the alerts that should
/// be active with those that are, raising and resolving the difference, so it corrects itself if
/// an earlier evaluation was missed (for example after a crash).
/// </summary>
/// <remarks>
/// Per cost centre the RFQ asks for three levels, and only the most severe one is active:
/// no full cylinder (immediately), the last full cylinder, and at or below the low-stock threshold.
/// </remarks>
public sealed class StockAlertEvaluator(IBoxStore store, BoxAlerts alerts)
{
    private static readonly AlertKind[] StockLevels = [AlertKind.LowStock, AlertKind.LastFullCylinder, AlertKind.NoFullCylinder];

    public async Task EvaluateAsync(int boxId, CancellationToken cancellationToken)
    {
        var inventory = await store.LoadInventoryAsync(boxId, cancellationToken);
        var settings = await store.GetSettingsAsync(cancellationToken);
        var fullCounts = inventory.FullCylinders()
            .GroupBy(entry => entry.Cylinder.CostCentreId)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var costCentre in await store.GetCostCentresServedByAsync(boxId, cancellationToken))
        {
            var count = fullCounts.GetValueOrDefault(costCentre.Id);
            var threshold = costCentre.LowStockThreshold ?? settings.LowStockThreshold;
            AlertKind? level = count switch
            {
                0 => AlertKind.NoFullCylinder,
                1 => AlertKind.LastFullCylinder,
                _ when count <= threshold => AlertKind.LowStock,
                _ => null,
            };

            foreach (var kind in StockLevels)
            {
                var facts = new AlertFacts(kind, inventory.Box.Code, costCentre, count);
                if (kind == level)
                {
                    await alerts.RaiseAsync(facts, cancellationToken);
                }
                else
                {
                    await alerts.ResolveAsync(facts, cancellationToken);
                }
            }
        }
    }
}
