using Microsoft.Data.Sqlite;

namespace OxBox.Infrastructure.Persistence;

/// <summary>
/// The SQLite file that holds all data. A relative <c>Data Source</c> is taken from the app's
/// folder, so the database stays next to the app however it is started.
/// </summary>
public sealed record SqliteDatabaseFile(string ConnectionString, string Path)
{
    /// <summary>Makes the file path absolute and creates its folder; SQLite creates the file itself.</summary>
    public static SqliteDatabaseFile Prepare(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        builder.DataSource = System.IO.Path.GetFullPath(builder.DataSource, basePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(builder.DataSource)!);
        return new SqliteDatabaseFile(builder.ConnectionString, builder.DataSource);
    }
}
