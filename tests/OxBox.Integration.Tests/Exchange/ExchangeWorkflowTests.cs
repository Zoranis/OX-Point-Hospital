using Microsoft.EntityFrameworkCore;
using OxBox.Application.Exchange;
using OxBox.Application.Feedback;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Transactions;
using OxBox.Infrastructure.Persistence;
using OxBox.Integration.Tests.Boxes;
using OxBox.Integration.Tests.Persistence;
using static OxBox.Integration.Tests.Boxes.BoxHarness;

namespace OxBox.Integration.Tests.Exchange;

/// <summary>
/// Drives the exchange workflow end to end against PostgreSQL and the demo data set (see
/// <see cref="BoxHarness"/>). Every scan runs in a fresh unit of work, as in the box agent.
/// </summary>
[Collection(PostgresTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class ExchangeWorkflowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private BoxHarness _box = null!;

    private FakeBoxGateway Gateway => _box.Gateway;

    private KioskScreen? LastScreen => _box.Display.Last;

    public async Task InitializeAsync() => _box = await BoxHarness.CreateAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Exchange_HappyPath_UnlocksBothSlotsAndRecordsBoth()
    {
        await _box.ScanAsync("O2-INT-04");

        Gateway.OpenedLocks.Should().Equal(Slot(8), Slot(1));
        LastScreen.Should().Be(new ExchangeOpenScreen(ReturnPositionNumber: 8, DispensePositionNumber: 1));
        Gateway.PulsedRelays.Should().Equal(BoxSignals.GreenLamp);

        await using var db = _box.Db();
        var transactions = await NewTransactionsAsync(db);
        transactions.Select(t => (t.Action, t.Barcode, t.CostCentreCode, t.BoxCode, t.PositionNumber, t.FlagAtTime)).Should().Equal(
            (CylinderAction.Return, "O2-INT-04", "1101", "BOX-01", 8, 8441),
            (CylinderAction.Dispense, "O2-INT-01", "1101", "BOX-01", 1, 9201));
        (await CylinderAsync(db, "O2-INT-04")).Should().Match<Cylinder>(c => c.Status == CylinderStatus.Empty && c.Location == CylinderLocation.InBox);
        (await CylinderAsync(db, "O2-INT-01")).Should().Match<Cylinder>(c => c.Status == CylinderStatus.Full && c.Location == CylinderLocation.AtCostCentre);
    }

    [Fact]
    public async Task Scan_UnknownBarcode_OpensNothingAndSignalsError()
    {
        await _box.ScanAsync("NEZNAMY-999");

        Gateway.OpenedLocks.Should().BeEmpty();
        LastScreen.Should().Be(new RejectedScreen(ExchangeError.UnknownBarcode));
        Gateway.PulsedRelays.Should().Equal(BoxSignals.RedLampAndBuzzer);
    }

    [Fact]
    public async Task Scan_DeactivatedCylinder_IsTreatedAsUnknown()
    {
        await using (var db = _box.Db())
        {
            (await CylinderAsync(db, "O2-INT-04")).Deactivate();
            await db.SaveChangesAsync();
        }

        await _box.ScanAsync("O2-INT-04");

        Gateway.OpenedLocks.Should().BeEmpty();
        LastScreen.Should().Be(new RejectedScreen(ExchangeError.UnknownBarcode));
    }

    [Fact]
    public async Task Scan_SameCylinderTwice_SecondIsRejectedAsAlreadyReturned()
    {
        await _box.ScanAsync("O2-INT-04");
        await _box.ScanAsync("O2-INT-04");

        Gateway.OpenedLocks.Should().HaveCount(2, "only the first scan opens anything");
        LastScreen.Should().Be(new RejectedScreen(ExchangeError.AlreadyReturned));
    }

    [Fact]
    public async Task Scan_CostCentresLastFullCylinderGone_IsRejected()
    {
        await _box.ScanAsync("O2-ARO-02");
        await _box.ScanAsync("O2-ARO-03");

        LastScreen.Should().Be(new RejectedScreen(ExchangeError.NoFullCylinderForCostCentre));
        Gateway.OpenedLocks.Should().Equal(Slot(8), Slot(7));
    }

    [Fact]
    public async Task ReturnLock_Jammed_RecordsNothing()
    {
        Gateway.Jam(Slot(8));

        await _box.ScanAsync("O2-INT-04");

        LastScreen.Should().Be(new RejectedScreen(ExchangeError.HardwareFault));
        await using var db = _box.Db();
        (await NewTransactionsAsync(db)).Should().BeEmpty();
        (await CylinderAsync(db, "O2-INT-04")).Location.Should().Be(CylinderLocation.AtCostCentre);
    }

    [Fact]
    public async Task DispenseLock_Jammed_KeepsTheReturnAndReportsDispenseFailed()
    {
        Gateway.Jam(Slot(1));

        await _box.ScanAsync("O2-INT-04");

        LastScreen.Should().Be(new RejectedScreen(ExchangeError.DispenseFailed, ReturnPositionNumber: 8));
        await using var db = _box.Db();
        (await NewTransactionsAsync(db)).Select(t => t.Action).Should().Equal(CylinderAction.Return);
        (await CylinderAsync(db, "O2-INT-01")).Location.Should().Be(CylinderLocation.InBox);
    }

    [Fact]
    public async Task Scan_BuDisconnected_RecordsNothing()
    {
        Gateway.IsConnected = false;

        await _box.ScanAsync("O2-INT-04");

        LastScreen.Should().Be(new RejectedScreen(ExchangeError.HardwareFault));
        await using var db = _box.Db();
        (await NewTransactionsAsync(db)).Should().BeEmpty();
    }

    // The demo data's restocking into the box is recorded as RestockIn; exchanges add the rest.
    private static Task<List<CylinderTransaction>> NewTransactionsAsync(OxBoxDbContext db) =>
        db.CylinderTransactions.Where(t => t.Action != CylinderAction.RestockIn).OrderBy(t => t.Id).ToListAsync();

    private static Task<Cylinder> CylinderAsync(OxBoxDbContext db, string barcode) => db.Cylinders.SingleAsync(c => c.Barcode == barcode);
}
