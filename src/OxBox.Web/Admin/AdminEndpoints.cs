using Microsoft.EntityFrameworkCore;
using OxBox.Infrastructure.Persistence;
using OxBox.Infrastructure.Reports;

namespace OxBox.Web.Admin;

/// <summary>File downloads for the web app (CSV exports).</summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var export = app.MapGroup("/export").RequireAuthorization();
        export.MapGet("/transactions.csv", ExportTransactionsAsync);
        export.MapGet("/dispensed.csv", ExportDispensedAsync);
    }

    private static async Task<IResult> ExportTransactionsAsync(
        HttpContext context,
        IDbContextFactory<OxBoxDbContext> contexts,
        SiteClock clock,
        CancellationToken cancellationToken)
    {
        var scope = UserScope.From(context.User);
        var filter = TransactionFilter.FromQuery(context.Request.Query);
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var transactions = await filter.Apply(db.CylinderTransactions.AsNoTracking(), clock, scope).Take(100_000).ToListAsync(cancellationToken);

        var csv = ReportExport.Csv(
            ["Time", "Box", "Slot", "Barcode", "Cost centre", "Action", "Flag"],
            transactions.Select(t => (IReadOnlyList<object>)
                [clock.Local(t.AtUtc).DateTime, t.BoxCode, t.PositionNumber, t.Barcode, t.CostCentreCode, t.Action.ToString(), t.FlagAtTime]));
        return Results.File(csv, "text/csv", $"oxbox-transactions-{clock.Today:yyyy-MM-dd}.csv");
    }

    private static async Task<IResult> ExportDispensedAsync(
        HttpContext context,
        ReportQueries reports,
        SiteClock clock,
        CancellationToken cancellationToken)
    {
        var scope = UserScope.From(context.User);
        var request = ReportRequest.FromQuery(context.Request.Query, clock.Today);
        var rows = await reports.DispensedAsync(
            request.Period, new ReportFilter(request.BoxCode, scope.AllowedCostCentres, request.CostCentreCode), cancellationToken);

        var csv = ReportExport.Csv(
            ["Cost centre", "Name", "Full cylinders dispensed"],
            rows.Select(row => (IReadOnlyList<object>)[row.CostCentreCode, row.CostCentreName, row.Dispensed]));
        return Results.File(csv, "text/csv", $"oxbox-dispensed-{request.Period.ToString().Replace(' ', '_')}.csv");
    }
}
