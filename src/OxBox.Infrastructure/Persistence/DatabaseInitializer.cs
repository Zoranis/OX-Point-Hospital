using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OxBox.Domain.Boxes;
using OxBox.Domain.Settings;

namespace OxBox.Infrastructure.Persistence;

/// <summary>
/// Brings the database to a usable state at startup. Every step is idempotent, so it is safe
/// to run on every start.
/// </summary>
public sealed partial class DatabaseInitializer(
    OxBoxDbContext db,
    DemoDataSeeder demoData,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (options.Value.MigrateOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        // Write-ahead logging lets the admin pages read while a box agent writes. The mode is
        // stored in the file, so this only changes something on the first start.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

        await EnsureSystemSettingsAsync(cancellationToken);
        await EnsureDefaultBoxAsync(cancellationToken);

        if (options.Value.SeedDemoData)
        {
            await demoData.SeedAsync(options.Value.DemoBoxBarcodes, cancellationToken);
        }
    }

    private async Task EnsureSystemSettingsAsync(CancellationToken cancellationToken)
    {
        if (await db.SystemSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        db.SystemSettings.Add(SystemSettings.Defaults());
        await db.SaveChangesAsync(cancellationToken);
        LogCreatedSettings();
    }

    private async Task EnsureDefaultBoxAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value.DefaultBox;
        if (seed is null || await db.Boxes.AnyAsync(cancellationToken))
        {
            return;
        }

        var box = Box.CreateWithSequentialWiring(seed.Code, seed.Name, seed.Site, seed.BuHost, seed.BuPort, seed.PositionCount);
        db.Boxes.Add(box);
        await db.SaveChangesAsync(cancellationToken);
        LogCreatedBox(box.Code, box.Positions.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created default system settings")]
    private partial void LogCreatedSettings();

    [LoggerMessage(Level = LogLevel.Information, Message = "Created box {BoxCode} with {PositionCount} positions")]
    private partial void LogCreatedBox(string boxCode, int positionCount);
}
