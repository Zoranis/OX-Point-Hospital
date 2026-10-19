using System.Globalization;

namespace OxBox.Web.Admin;

/// <summary>
/// Shows times in the site's display time zone (Settings page, Europe/Prague by default). Stored
/// times are UTC; people read local time.
/// </summary>
public sealed class SiteClock(TimeProvider time)
{
    private TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    public TimeZoneInfo Zone => _zone;

    public DateTimeOffset Now => Local(time.GetUtcNow());

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    /// <summary>Called at startup and whenever the setting changes.</summary>
    public void UseZone(TimeZoneInfo zone) => _zone = zone;

    public DateTimeOffset Local(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, _zone);

    public string Format(DateTimeOffset utc) => Local(utc).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public string FormatWithSeconds(DateTimeOffset utc) => Local(utc).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>The UTC instant of local midnight at the start of <paramref name="day"/>.</summary>
    public DateTimeOffset StartOfDayUtc(DateOnly day) =>
        new(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), _zone), TimeSpan.Zero);
}
