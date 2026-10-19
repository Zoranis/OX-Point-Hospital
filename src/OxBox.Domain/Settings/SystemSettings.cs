namespace OxBox.Domain.Settings;

/// <summary>Site-wide settings, stored as a single row and editable in the web app.</summary>
public sealed class SystemSettings
{
    public const int SingletonId = 1;

    private SystemSettings()
    {
        DisplayTimeZoneId = null!;
    }

    public SystemSettings(int fullFlag, int emptyFlag, string displayTimeZoneId)
    {
        Id = SingletonId;
        DisplayTimeZoneId = null!;
        ChangeFlags(fullFlag, emptyFlag);
        ChangeDisplayTimeZone(displayTimeZoneId);
        LowStockThreshold = 2;
    }

    public int Id { get; private set; }

    /// <summary>Cost centre number recorded for full cylinders (hospital campus). Default 9201.</summary>
    public int FullFlag { get; private set; }

    /// <summary>Cost centre number recorded for empty cylinders (energy department). Default 8441.</summary>
    public int EmptyFlag { get; private set; }

    /// <summary>IANA time zone used to display times and to cut report periods, e.g. "Europe/Prague".</summary>
    public string DisplayTimeZoneId { get; private set; }

    /// <summary>
    /// Alert when this many full cylinders or fewer are left for a cost centre in a box, unless
    /// the cost centre sets its own threshold. Default 2.
    /// </summary>
    public int LowStockThreshold { get; private set; }

    public TimeZoneInfo DisplayTimeZone => TimeZoneInfo.FindSystemTimeZoneById(DisplayTimeZoneId);

    public static SystemSettings Defaults() => new(fullFlag: 9201, emptyFlag: 8441, displayTimeZoneId: "Europe/Prague");

    public void ChangeFlags(int fullFlag, int emptyFlag)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fullFlag);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(emptyFlag);
        if (fullFlag == emptyFlag)
        {
            throw new ArgumentException("The full and empty flags must differ.", nameof(emptyFlag));
        }

        FullFlag = fullFlag;
        EmptyFlag = emptyFlag;
    }

    public void ChangeLowStockThreshold(int threshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        LowStockThreshold = threshold;
    }

    public void ChangeDisplayTimeZone(string displayTimeZoneId)
    {
        var id = Guard.NotBlank(displayTimeZoneId, nameof(displayTimeZoneId));
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
        {
            throw new ArgumentException($"Unknown time zone '{id}'.", nameof(displayTimeZoneId));
        }

        DisplayTimeZoneId = id;
    }
}
