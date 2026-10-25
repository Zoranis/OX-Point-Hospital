using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OxBox.Application.Reports;
using OxBox.Domain.Transactions;
using OxBox.Infrastructure.Persistence;
using OxBox.Infrastructure.Reports;
using OxBox.Integration.Tests.Persistence;

namespace OxBox.Integration.Tests.Reports;

[Collection(DatabaseTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class ReportQueriesTests(SqliteFixture database) : IAsyncLifetime
{
    private string _connectionString = null!;
    private ReportQueries _reports = null!;

    public async Task InitializeAsync()
    {
        var defaultBox = new DefaultBoxOptions { Code = "BOX-01", Name = "Box", BuHost = "localhost", PositionCount = 30 };
        _connectionString = await database.CreateMigratedDatabaseAsync(defaultBox, seedDemoData: true);
        var options = new DbContextOptionsBuilder<OxBoxDbContext>().UseSqlite(_connectionString).Options;
        _reports = new ReportQueries(new PooledDbContextFactory<OxBoxDbContext>(options));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Dispensed_CutsMonthsAtPragueMidnightNotUtc()
    {
        // Prague is UTC+1 on 31 October 2026 (summer time ended on the 25th).
        await AddDispenseAsync("1101", new DateTimeOffset(2026, 10, 31, 22, 30, 0, TimeSpan.Zero)); // 31 Oct 23:30 Prague
        await AddDispenseAsync("1101", new DateTimeOffset(2026, 10, 31, 23, 30, 0, TimeSpan.Zero)); // 1 Nov 00:30 Prague
        await AddDispenseAsync("1202", new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero));

        var november = await _reports.DispensedAsync(ReportPeriod.Month(2026, 11), new ReportFilter(BoxCode: null, AllowedCostCentres: null), CancellationToken.None);
        var october = await _reports.DispensedAsync(ReportPeriod.Month(2026, 10), new ReportFilter(BoxCode: null, AllowedCostCentres: null), CancellationToken.None);

        november.Select(r => (r.CostCentreCode, r.Dispensed)).Should().Equal(("1101", 1), ("1202", 1), ("1305", 0));
        october.Single(r => r.CostCentreCode == "1101").Dispensed.Should().Be(1);
    }

    [Fact]
    public async Task Dispensed_ForAViewer_ListsOnlyTheirCostCentres()
    {
        var rows = await _reports.DispensedAsync(ReportPeriod.Month(2026, 11), new ReportFilter(null, ["1305"]), CancellationToken.None);

        rows.Select(r => r.CostCentreCode).Should().Equal("1305");
    }

    [Fact]
    public async Task CylinderCounts_MatchTheDemoData()
    {
        var rows = await _reports.CylinderCountsAsync(new ReportFilter(null, null), CancellationToken.None);

        var aro = rows.Single(r => r.CostCentreCode == "1305");
        aro.Should().Match<CylinderCountRow>(r => r.FullInBox == 1 && r.FullAtCostCentre == 2 && r.WithSupplier == 2 && r.FullOnCampus == 3);
    }

    private async Task AddDispenseAsync(string costCentre, DateTimeOffset atUtc)
    {
        await using var db = SqliteFixture.CreateDbContext(_connectionString);
        db.CylinderTransactions.Add(new CylinderTransaction(atUtc, CylinderAction.Dispense, "BOX-01", 1, "O2-TEST", costCentre, 9201));
        await db.SaveChangesAsync();
    }
}
