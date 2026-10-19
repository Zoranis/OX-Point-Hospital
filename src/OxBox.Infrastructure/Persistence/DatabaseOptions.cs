using OxBox.OxPointProtocol;

namespace OxBox.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Applies pending EF migrations at startup. Convenient for a single on-prem server.
    /// Turn it off if migrations are deployed separately.
    /// </summary>
    public bool MigrateOnStartup { get; set; } = true;

    /// <summary>
    /// Fills an empty database with demo cost centres, cylinders and a technician whose barcodes
    /// match the BU simulator. For demonstrations only; never enable it in production.
    /// </summary>
    public bool SeedDemoData { get; set; }

    /// <summary>Creates this box on first start if no box exists yet. Leave unset to skip.</summary>
    public DefaultBoxOptions? DefaultBox { get; set; }
}

/// <remarks>
/// Values are validated by <see cref="Domain.Boxes.Box.CreateWithSequentialWiring"/> when the box is created.
/// </remarks>
public sealed class DefaultBoxOptions
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Site { get; set; } = string.Empty;

    public string BuHost { get; set; } = string.Empty;

    public int BuPort { get; set; } = OxPointLimits.DefaultPort;

    /// <summary>The RFQ box has 30 positions: CU0 locks 1–16 and CU1 locks 1–14.</summary>
    public int PositionCount { get; set; } = 30;
}
