using System.Globalization;

namespace OxBox.Application.Reports;

/// <summary>
/// A reporting period in local calendar days, for example a month for invoicing. It is cut at
/// local midnight in the display time zone (Europe/Prague), so a cylinder dispensed at 00:30 on
/// 1 November belongs to November even though it is still 31 October in UTC.
/// </summary>
public sealed record ReportPeriod
{
    private ReportPeriod(DateOnly from, DateOnly toExclusive)
    {
        if (toExclusive <= from)
        {
            throw new ArgumentException("A period must end after it starts.", nameof(toExclusive));
        }

        From = from;
        ToExclusive = toExclusive;
    }

    /// <summary>The first day of the period.</summary>
    public DateOnly From { get; }

    /// <summary>The day after the last day of the period.</summary>
    public DateOnly ToExclusive { get; }

    public DateOnly ToInclusive => ToExclusive.AddDays(-1);

    public static ReportPeriod Month(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        return new ReportPeriod(first, first.AddMonths(1));
    }

    public static ReportPeriod Days(DateOnly from, DateOnly toInclusive) => new(from, toInclusive.AddDays(1));

    /// <summary>The period as a UTC range, start inclusive and end exclusive.</summary>
    public (DateTimeOffset FromUtc, DateTimeOffset ToUtc) ToUtc(TimeZoneInfo timeZone) =>
        (MidnightUtc(From, timeZone), MidnightUtc(ToExclusive, timeZone));

    public override string ToString() =>
        From.Day == 1 && ToExclusive == From.AddMonths(1)
            ? From.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : $"{From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} to {ToInclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static DateTimeOffset MidnightUtc(DateOnly day, TimeZoneInfo timeZone)
    {
        var localMidnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone), TimeSpan.Zero);
    }
}
