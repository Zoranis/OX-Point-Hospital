using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using OxBox.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace OxBox.Integration.Tests.Persistence;

/// <summary>One PostgreSQL container for the whole test run. Each test class gets its own database.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates an empty database, applies all migrations and seeds it as the app would at startup.</summary>
    public async Task<string> CreateMigratedDatabaseAsync(DefaultBoxOptions? defaultBox = null, bool seedDemoData = false)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = $"oxbox_test_{Guid.NewGuid():N}",
        }.ConnectionString;

        await using var db = CreateDbContext(connectionString);
        await InitializeAsync(db, defaultBox, seedDemoData);
        return connectionString;
    }

    public static OxBoxDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<OxBoxDbContext>().UseNpgsql(connectionString).Options);

    public static Task InitializeAsync(OxBoxDbContext db, DefaultBoxOptions? defaultBox, bool seedDemoData = false)
    {
        var options = Options.Create(new DatabaseOptions { MigrateOnStartup = true, DefaultBox = defaultBox, SeedDemoData = seedDemoData });
        var demoData = new DemoDataSeeder(db, TimeProvider.System, NullLogger<DemoDataSeeder>.Instance);
        return new DatabaseInitializer(db, demoData, options, NullLogger<DatabaseInitializer>.Instance)
            .InitializeAsync(CancellationToken.None);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
