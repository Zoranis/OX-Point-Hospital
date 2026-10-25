using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OxBox.Infrastructure.Persistence;

namespace OxBox.Integration.Tests.Persistence;

/// <summary>A temporary folder of SQLite files for the whole test run. Each test class gets its own database file.</summary>
public sealed class SqliteFixture : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"oxbox_tests_{Guid.NewGuid():N}");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Pooled connections keep the files open.
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
        return Task.CompletedTask;
    }

    /// <summary>Creates an empty database, applies all migrations and seeds it as the app would at startup.</summary>
    public async Task<string> CreateMigratedDatabaseAsync(DefaultBoxOptions? defaultBox = null, bool seedDemoData = false)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_folder, $"oxbox_test_{Guid.NewGuid():N}.db"),
            DefaultTimeout = 30,
        }.ConnectionString;

        await using var db = CreateDbContext(connectionString);
        await InitializeAsync(db, defaultBox, seedDemoData);
        return connectionString;
    }

    public static OxBoxDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<OxBoxDbContext>().UseSqlite(connectionString).Options);

    public static Task InitializeAsync(OxBoxDbContext db, DefaultBoxOptions? defaultBox, bool seedDemoData = false)
    {
        var options = Options.Create(new DatabaseOptions { MigrateOnStartup = true, DefaultBox = defaultBox, SeedDemoData = seedDemoData });
        var demoData = new DemoDataSeeder(db, TimeProvider.System, NullLogger<DemoDataSeeder>.Instance);
        return new DatabaseInitializer(db, demoData, options, NullLogger<DatabaseInitializer>.Instance)
            .InitializeAsync(CancellationToken.None);
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseTestGroup : ICollectionFixture<SqliteFixture>
{
    public const string Name = "Database";
}
