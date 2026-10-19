using Microsoft.EntityFrameworkCore;
using OxBox.Application.Feedback;
using OxBox.Application.Service;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Transactions;
using OxBox.Integration.Tests.Boxes;
using OxBox.Integration.Tests.Persistence;
using static OxBox.Integration.Tests.Boxes.BoxHarness;

namespace OxBox.Integration.Tests.Service;

/// <summary>A technician's restocking visit, end to end, on the demo box (see <see cref="BoxHarness"/>).</summary>
[Collection(PostgresTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class ServiceWorkflowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ServiceCard = "SERVIS-001";

    private BoxHarness _box = null!;

    private KioskScreen? LastScreen => _box.Display.Last;

    public async Task InitializeAsync() => _box = await BoxHarness.CreateAsync(postgres);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Visit_HappyPath_TakesOutEmptiesRestocksAndEnds()
    {
        // ARO returns its empty to slot 8 and takes the full one from slot 7.
        await _box.ScanAsync("O2-ARO-02");

        await _box.ScanAsync(ServiceCard);
        _box.Box.Visit.Should().NotBeNull();
        LastScreen.Should().Be(new ServiceScreen("Jan Novák", [8], RestockedCount: 0, LastInsertedPositionNumber: null));

        await _box.ScanAsync("O2-ARO-04");
        LastScreen.Should().Be(new ServiceScreen("Jan Novák", [8], RestockedCount: 1, LastInsertedPositionNumber: 7));

        await _box.ScanAsync(ServiceCard);
        _box.Box.Visit.Should().BeNull();
        LastScreen.Should().Be(new ServiceFinishedScreen(RemovedCount: 1, RestockedCount: 1));

        _box.Gateway.OpenedLocks.Should().Equal(Slot(8), Slot(7), Slot(8), Slot(7));
        await using var db = _box.Db();
        var technicianId = (await db.Technicians.SingleAsync()).Id;
        var visit = await db.CylinderTransactions.Where(t => t.TechnicianId != null).OrderBy(t => t.Id).ToListAsync();
        visit.Select(t => (t.Action, t.Barcode, t.PositionNumber, t.FlagAtTime, t.TechnicianId)).Should().Equal(
            (CylinderAction.RestockOut, "O2-ARO-02", 8, 8441, technicianId),
            (CylinderAction.RestockIn, "O2-ARO-04", 7, 9201, technicianId));
        (await db.Cylinders.SingleAsync(c => c.Barcode == "O2-ARO-02")).Location.Should().Be(CylinderLocation.WithSupplier);
        (await db.Cylinders.SingleAsync(c => c.Barcode == "O2-ARO-04")).Should()
            .Match<Cylinder>(c => c.Status == CylinderStatus.Full && c.Location == CylinderLocation.InBox);
    }

    [Fact]
    public async Task Visit_NoEmptiesInBox_OpensNothingAndWaitsForFullCylinders()
    {
        await _box.ScanAsync(ServiceCard);

        _box.Gateway.OpenedLocks.Should().BeEmpty();
        LastScreen.Should().Be(new ServiceScreen("Jan Novák", [], RestockedCount: 0, LastInsertedPositionNumber: null));
    }

    [Fact]
    public async Task Scan_UnknownBarcode_IsRefusedAndOpensNothing()
    {
        await _box.ScanAsync(ServiceCard);
        await _box.ScanAsync("NEZNAMY-999");

        LastScreen.Should().BeOfType<ServiceRejectedScreen>().Which.Error.Should().Be(ServiceError.UnknownBarcode);
        _box.Gateway.OpenedLocks.Should().BeEmpty();
    }

    [Fact]
    public async Task Scan_CylinderAtItsDepartment_IsRefusedSoAnEmptyNeverBecomesFull()
    {
        await _box.ScanAsync(ServiceCard);
        await _box.ScanAsync("O2-INT-04");

        LastScreen.Should().BeOfType<ServiceRejectedScreen>().Which.Error.Should().Be(ServiceError.AtCostCentre);
        await using var db = _box.Db();
        (await db.Cylinders.SingleAsync(c => c.Barcode == "O2-INT-04")).Location.Should().Be(CylinderLocation.AtCostCentre);
    }

    [Fact]
    public async Task InsertLock_Jammed_RecordsNothing()
    {
        _box.Gateway.Jam(Slot(8));
        await _box.ScanAsync(ServiceCard);
        await _box.ScanAsync("O2-ARO-04");

        LastScreen.Should().BeOfType<ServiceRejectedScreen>().Which.Error.Should().Be(ServiceError.NoSlotOpened);
        await using var db = _box.Db();
        (await db.Cylinders.SingleAsync(c => c.Barcode == "O2-ARO-04")).Location.Should().Be(CylinderLocation.WithSupplier);
    }

    [Fact]
    public async Task Visit_Inactive_EndsByItself()
    {
        await _box.ScanAsync(ServiceCard);

        _box.Time.Advance(_box.ServiceOptions.InactivityTimeout - TimeSpan.FromSeconds(1));
        await _box.TickAsync();
        _box.Box.Visit.Should().NotBeNull();

        _box.Time.Advance(TimeSpan.FromSeconds(1));
        await _box.TickAsync();
        _box.Box.Visit.Should().BeNull();
        LastScreen.Should().Be(new ServiceFinishedScreen(RemovedCount: 0, RestockedCount: 0));
    }
}
