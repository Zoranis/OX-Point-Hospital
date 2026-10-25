using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OxBox.Application.Abstractions;
using OxBox.Application.Alerts;
using OxBox.Application.Boxes;
using OxBox.Application.Exchange;
using OxBox.Application.Feedback;
using OxBox.Application.Inventory;
using OxBox.Application.Service;
using OxBox.Infrastructure.Persistence;
using OxBox.Integration.Tests.Exchange;
using OxBox.Integration.Tests.Persistence;
using OxBox.OxPointProtocol;

namespace OxBox.Integration.Tests.Boxes;

/// <summary>
/// One demo box on its own database, driven the way its agent drives it: every event goes
/// through <see cref="BoxEvents"/> in a fresh unit of work, against an in-memory BU and a fake clock.
/// </summary>
/// <remarks>
/// The demo data: positions 1–3 hold internal medicine's full cylinders, 4–6 surgery's, 7 ARO's
/// only one, and 8–30 are free. O2-INT-04..06 are out at internal medicine, O2-ARO-04..05 are
/// refilled at the supplier, and SERVIS-001 is technician Jan Novák's service code.
/// </remarks>
internal sealed class BoxHarness
{
    private BoxHarness(string connectionString, int boxId)
    {
        ConnectionString = connectionString;
        var hardware = new BoxHardware("BOX-01", Gateway, openAttempts: 3, NullLogger<BoxHardware>.Instance);
        Box = new BoxLink(boxId, "BOX-01", hardware, new BoxSignals("BOX-01", hardware, Display, NullLogger<BoxSignals>.Instance));
    }

    public FakeBoxGateway Gateway { get; } = new();

    public RecordingDisplay Display { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero));

    public ServiceOptions ServiceOptions { get; } = new();

    public string ConnectionString { get; }

    public BoxLink Box { get; }

    public static async Task<BoxHarness> CreateAsync(SqliteFixture database)
    {
        var defaultBox = new DefaultBoxOptions { Code = "BOX-01", Name = "Box", BuHost = "localhost", PositionCount = 30 };
        var connectionString = await database.CreateMigratedDatabaseAsync(defaultBox, seedDemoData: true);

        await using var db = SqliteFixture.CreateDbContext(connectionString);
        return new BoxHarness(connectionString, (await db.Boxes.SingleAsync()).Id);
    }

    /// <summary>Positions are wired in order: slots 1–16 on CU0, 17–30 on CU1.</summary>
    public static LockAddress Slot(int number) => new((number - 1) / 16, ((number - 1) % 16) + 1);

    public OxBoxDbContext Db() => SqliteFixture.CreateDbContext(ConnectionString);

    public Task ScanAsync(string code) => WithEventsAsync(events => events.ScannedAsync(Box, code, CancellationToken.None));

    public Task TickAsync() => WithEventsAsync(events => events.TickAsync(Box, CancellationToken.None));

    private async Task WithEventsAsync(Func<BoxEvents, Task> handle)
    {
        await using var db = Db();
        IBoxStore store = new EfBoxStore(db);
        var ledger = new CylinderLedger(store, Time, NullLogger<CylinderLedger>.Instance);
        var alerts = new BoxAlerts(store, Time, NullLogger<BoxAlerts>.Instance);
        var events = new BoxEvents(
            store,
            new ExchangeWorkflow(store, ledger, NullLogger<ExchangeWorkflow>.Instance),
            new ServiceWorkflow(store, ledger, Options.Create(ServiceOptions), Time, NullLogger<ServiceWorkflow>.Instance),
            new StockAlertEvaluator(store, alerts));
        await handle(events);
    }
}
