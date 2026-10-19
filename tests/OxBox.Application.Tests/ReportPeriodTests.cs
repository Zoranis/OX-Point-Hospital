using OxBox.Application.Reports;

namespace OxBox.Application.Tests;

public class ReportPeriodTests
{
    private static readonly TimeZoneInfo Prague = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");

    [Fact]
    public void Month_InWinter_StartsAtPragueMidnight()
    {
        var (fromUtc, toUtc) = ReportPeriod.Month(2026, 11).ToUtc(Prague);

        fromUtc.Should().Be(new DateTimeOffset(2026, 10, 31, 23, 0, 0, TimeSpan.Zero));
        toUtc.Should().Be(new DateTimeOffset(2026, 11, 30, 23, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Month_AcrossTheSummerTimeChange_UsesEachMidnightsOwnOffset()
    {
        var (fromUtc, toUtc) = ReportPeriod.Month(2026, 10).ToUtc(Prague);

        fromUtc.Should().Be(new DateTimeOffset(2026, 9, 30, 22, 0, 0, TimeSpan.Zero));
        toUtc.Should().Be(new DateTimeOffset(2026, 10, 31, 23, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Days_IncludeTheLastDay()
    {
        var period = ReportPeriod.Days(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        period.ToExclusive.Should().Be(new DateOnly(2026, 10, 16));
        period.ToString().Should().Be("2026-10-01 to 2026-10-15");
    }

    [Fact]
    public void Days_EndingBeforeTheyStart_Throw()
    {
        var act = () => ReportPeriod.Days(new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 1));

        act.Should().Throw<ArgumentException>();
    }
}
