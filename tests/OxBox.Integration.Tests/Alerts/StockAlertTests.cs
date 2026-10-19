using Microsoft.EntityFrameworkCore;
using OxBox.Domain.Alerts;
using OxBox.Integration.Tests.Boxes;
using OxBox.Integration.Tests.Persistence;

namespace OxBox.Integration.Tests.Alerts;

/// <summary>
/// Stock alerts follow what is in the box. In the demo data ARO starts with one full cylinder, and
/// the one alert recipient has both an e-mail address and a phone number.
/// </summary>
[Collection(PostgresTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class StockAlertTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string AroLastFull = "LastFullCylinder:BOX-01:1305";
    private const string AroNoFull = "NoFullCylinder:BOX-01:1305";

    private BoxHarness _box = null!;

    public async Task InitializeAsync() => _box = await BoxHarness.CreateAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnyEvent_CostCentreDownToItsLastCylinder_RaisesOneAlertWithAnEmailAndAnSms()
    {
        await _box.ScanAsync("NEZNAMY-999");

        await using var db = _box.Db();
        var alert = await db.Alerts.SingleAsync(a => a.Key == AroLastFull);
        alert.IsActive.Should().BeTrue();
        (await db.OutboxMessages.Where(m => m.AlertId == alert.Id).Select(m => m.Channel).ToListAsync())
            .Should().BeEquivalentTo([MessageChannel.Email, MessageChannel.Sms]);
    }

    [Fact]
    public async Task LastCylinderDispensed_EscalatesToNoFullCylinderAndSendsEachOnlyOnce()
    {
        await _box.ScanAsync("NEZNAMY-999");
        await _box.ScanAsync("O2-ARO-02");

        await _box.ScanAsync("NEZNAMY-999");
        await _box.ScanAsync("NEZNAMY-999");

        await using var db = _box.Db();
        (await db.Alerts.SingleAsync(a => a.Key == AroLastFull)).IsActive.Should().BeFalse();
        (await db.Alerts.CountAsync(a => a.Key == AroNoFull)).Should().Be(1);
        (await db.Alerts.SingleAsync(a => a.Key == AroNoFull)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Restocked_ResolvesTheAlertAndRaisesTheNewLevel()
    {
        await _box.ScanAsync("NEZNAMY-999");
        await _box.ScanAsync("O2-ARO-02");

        await _box.ScanAsync("SERVIS-001");
        await _box.ScanAsync("O2-ARO-04");
        await _box.ScanAsync("SERVIS-001");

        await using var db = _box.Db();
        (await db.Alerts.SingleAsync(a => a.Key == AroNoFull)).IsActive.Should().BeFalse();
        (await db.Alerts.Where(a => a.Key == AroLastFull).OrderBy(a => a.Id).Select(a => a.IsActive).ToListAsync())
            .Should().Equal(false, true);
    }

    [Fact]
    public async Task Restocking_DoesNotAlertHalfwayThroughTheVisit()
    {
        await _box.ScanAsync("NEZNAMY-999");
        await _box.ScanAsync("O2-ARO-02");

        // ARO goes from 0 to 2 full cylinders; at 1 it would be "last cylinder" if checked mid-visit.
        await _box.ScanAsync("SERVIS-001");
        await _box.ScanAsync("O2-ARO-04");
        await _box.ScanAsync("O2-ARO-05");
        await _box.ScanAsync("SERVIS-001");

        await using var db = _box.Db();
        (await db.Alerts.CountAsync(a => a.Key == AroLastFull)).Should().Be(1, "only the one raised before the visit");
        (await db.Alerts.SingleAsync(a => a.Key == "LowStock:BOX-01:1305")).IsActive.Should().BeTrue();
        (await db.Alerts.SingleAsync(a => a.Key == AroNoFull)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CostCentreThreshold_OverridesTheSiteWideOne()
    {
        await using (var db = _box.Db())
        {
            (await db.CostCentres.SingleAsync(c => c.Code == "1101")).ChangeLowStockThreshold(3);
            await db.SaveChangesAsync();
        }

        await _box.ScanAsync("NEZNAMY-999");

        await using var check = _box.Db();
        (await check.Alerts.AnyAsync(a => a.Key == "LowStock:BOX-01:1101" && a.ClearedAtUtc == null)).Should().BeTrue();
        (await check.Alerts.AnyAsync(a => a.Key == "LowStock:BOX-01:1202")).Should().BeFalse();
    }
}
