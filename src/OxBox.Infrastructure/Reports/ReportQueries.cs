using Microsoft.EntityFrameworkCore;
using OxBox.Application.Reports;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Transactions;
using OxBox.Infrastructure.Persistence;

namespace OxBox.Infrastructure.Reports;

/// <summary>
/// Every report definition, in one place. Reports count by action, status and location, never by
/// the full/empty flag numbers, so changing the flags in the settings never alters history.
/// </summary>
public sealed class ReportQueries(IDbContextFactory<OxBoxDbContext> contexts)
{
    /// <summary>The invoicing report: full cylinders dispensed per cost centre in the period.</summary>
    public async Task<IReadOnlyList<DispensedRow>> DispensedAsync(ReportPeriod period, ReportFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var counts = await CountTransactionsAsync(db, period, filter, [CylinderAction.Dispense], cancellationToken);
        var costCentres = await CostCentresAsync(db, filter, cancellationToken);
        return [.. costCentres.Select(c => new DispensedRow(c.Code, c.Name, counts.GetValueOrDefault((c.Code, CylinderAction.Dispense))))];
    }

    /// <summary>
    /// Empties collected in the period (flag 8441): returned by users, and taken out by
    /// technicians for refilling.
    /// </summary>
    public async Task<IReadOnlyList<EmptiesCollectedRow>> EmptiesCollectedAsync(ReportPeriod period, ReportFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        CylinderAction[] actions = [CylinderAction.Return, CylinderAction.RestockOut];
        var counts = await CountTransactionsAsync(db, period, filter, actions, cancellationToken);
        var costCentres = await CostCentresAsync(db, filter, cancellationToken);
        return
        [
            .. costCentres.Select(c => new EmptiesCollectedRow(
                c.Code,
                c.Name,
                counts.GetValueOrDefault((c.Code, CylinderAction.Return)),
                counts.GetValueOrDefault((c.Code, CylinderAction.RestockOut)))),
        ];
    }

    /// <summary>Where every cost centre's active cylinders are now.</summary>
    public async Task<IReadOnlyList<CylinderCountRow>> CylinderCountsAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var costCentres = await CostCentresAsync(db, filter, cancellationToken);
        var ids = costCentres.Select(c => c.Id).ToList();

        var groups = await db.Cylinders
            .Where(cylinder => cylinder.IsActive && ids.Contains(cylinder.CostCentreId))
            .GroupBy(cylinder => new { cylinder.CostCentreId, cylinder.Status, cylinder.Location })
            .Select(group => new { group.Key.CostCentreId, group.Key.Status, group.Key.Location, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(int costCentreId, CylinderStatus? status, CylinderLocation location) =>
            groups.Where(g => g.CostCentreId == costCentreId && g.Location == location && (status is null || g.Status == status)).Sum(g => g.Count);

        return
        [
            .. costCentres.Select(c => new CylinderCountRow(
                c.Code,
                c.Name,
                Count(c.Id, CylinderStatus.Full, CylinderLocation.AtCostCentre),
                Count(c.Id, CylinderStatus.Empty, CylinderLocation.AtCostCentre),
                Count(c.Id, CylinderStatus.Full, CylinderLocation.InBox),
                Count(c.Id, CylinderStatus.Empty, CylinderLocation.InBox),
                Count(c.Id, status: null, CylinderLocation.WithSupplier))),
        ];
    }

    private static async Task<Dictionary<(string Code, CylinderAction Action), int>> CountTransactionsAsync(
        OxBoxDbContext db,
        ReportPeriod period,
        ReportFilter filter,
        CylinderAction[] actions,
        CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking().SingleAsync(cancellationToken);
        var (fromUtc, toUtc) = period.ToUtc(settings.DisplayTimeZone);

        var query = db.CylinderTransactions.AsNoTracking()
            .Where(t => t.AtUtc >= fromUtc && t.AtUtc < toUtc && actions.Contains(t.Action));
        if (filter.BoxCode is { } boxCode)
        {
            query = query.Where(t => t.BoxCode == boxCode);
        }

        var counts = await query
            .GroupBy(t => new { t.CostCentreCode, t.Action })
            .Select(group => new { group.Key.CostCentreCode, group.Key.Action, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(c => (c.CostCentreCode, c.Action), c => c.Count);
    }

    private static async Task<List<CostCentre>> CostCentresAsync(OxBoxDbContext db, ReportFilter filter, CancellationToken cancellationToken)
    {
        var query = db.CostCentres.AsNoTracking();
        if (filter.AllowedCostCentres is { } allowed)
        {
            query = query.Where(c => allowed.Contains(c.Code));
        }

        if (filter.CostCentreCode is { } code)
        {
            query = query.Where(c => c.Code == code);
        }

        return await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }
}
