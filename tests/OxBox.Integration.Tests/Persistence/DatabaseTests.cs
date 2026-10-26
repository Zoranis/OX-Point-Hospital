using Microsoft.EntityFrameworkCore;
using OxBox.Domain.CostCentres;
using OxBox.Domain.Cylinders;
using OxBox.Domain.Settings;
using OxBox.Infrastructure.Persistence;
using OxBox.OxPointProtocol;

namespace OxBox.Integration.Tests.Persistence;

[Collection(DatabaseTestGroup.Name)]
[Trait("Category", "Integration")]
public sealed class DatabaseTests(SqliteFixture database)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private static readonly DefaultBoxOptions RfqBox = new()
    {
        Code = "BOX-01",
        Name = "Main box",
        BuHost = "10.0.0.5",
        PositionCount = 30,
    };

    [Fact]
    public async Task Initialize_FreshDatabase_CreatesSettingsAndThirtyPositionBox()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync(RfqBox);

        await using var db = SqliteFixture.CreateDbContext(connectionString);
        var settings = await db.SystemSettings.SingleAsync();
        var box = await db.Boxes.Include(b => b.Positions).SingleAsync();

        settings.Id.Should().Be(SystemSettings.SingletonId);
        settings.FullFlag.Should().Be(9201);
        box.Code.Should().Be("BOX-01");
        box.Positions.Should().HaveCount(30);
        box.Positions.Single(p => p.Number == 30).Lock.Should().Be(new LockAddress(1, 14));
    }

    [Fact]
    public async Task Initialize_DemoBoxBarcodesSet_PutsOnlyThoseCylindersFullInTheBox()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync(RfqBox, seedDemoData: true, demoBoxBarcodes: ["O2-ARO-04", "O2-ARO-05"]);

        await using var db = SqliteFixture.CreateDbContext(connectionString);
        var inBox = await db.Cylinders.Where(c => c.Location == CylinderLocation.InBox).ToListAsync();
        var slots = await db.Positions.ToDictionaryAsync(p => p.Id, p => p.Number);

        inBox.Select(c => (c.Barcode, slots[c.PositionId!.Value], c.Status)).Should().BeEquivalentTo(
            [("O2-ARO-04", 1, CylinderStatus.Full), ("O2-ARO-05", 2, CylinderStatus.Full)]);
    }

    [Fact]
    public async Task Initialize_RunTwice_DoesNotDuplicateData()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync(RfqBox);

        await using (var db = SqliteFixture.CreateDbContext(connectionString))
        {
            await SqliteFixture.InitializeAsync(db, RfqBox);
        }

        await using var check = SqliteFixture.CreateDbContext(connectionString);
        (await check.Boxes.CountAsync()).Should().Be(1);
        (await check.SystemSettings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SaveChanges_TwoCylindersInSamePosition_IsRejectedByDatabase()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync(RfqBox);
        await using var db = SqliteFixture.CreateDbContext(connectionString);
        var position = await db.Positions.FirstAsync();
        var costCentre = new CostCentre("1001", "Surgery");
        var first = new Cylinder("CYL-1", costCentre, CylinderStatus.Empty, CylinderLocation.WithSupplier);
        var second = new Cylinder("CYL-2", costCentre, CylinderStatus.Empty, CylinderLocation.WithSupplier);
        db.Cylinders.AddRange(first, second);
        await db.SaveChangesAsync();

        first.RestockIntoBox(position, Now);
        second.RestockIntoBox(position, Now);
        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChanges_CylinderChangedConcurrently_ThrowsConcurrencyException()
    {
        var connectionString = await database.CreateMigratedDatabaseAsync(RfqBox);
        await using (var setup = SqliteFixture.CreateDbContext(connectionString))
        {
            setup.Cylinders.Add(new Cylinder("CYL-1", new CostCentre("1001", "Surgery"), CylinderStatus.Full, CylinderLocation.AtCostCentre));
            await setup.SaveChangesAsync();
        }

        await using var serverA = SqliteFixture.CreateDbContext(connectionString);
        await using var serverB = SqliteFixture.CreateDbContext(connectionString);
        var cylinderSeenByA = await serverA.Cylinders.SingleAsync();
        var cylinderSeenByB = await serverB.Cylinders.SingleAsync();

        cylinderSeenByA.Deactivate();
        await serverA.SaveChangesAsync();
        cylinderSeenByB.ReturnToBox(await serverB.Positions.FirstAsync(), Now);
        var act = () => serverB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }
}
