using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using OxBox.Application.Reports;

namespace OxBox.Web.Admin;

/// <summary>The period and filters of the Reports page, also carried in its export links.</summary>
public sealed record ReportRequest(ReportPeriod Period, string? BoxCode, string? CostCentreCode)
{
    public string ToQueryString() => QueryHelpers.AddQueryString(string.Empty, new Dictionary<string, string?>
    {
        ["from"] = Period.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["to"] = Period.ToInclusive.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["box"] = BoxCode ?? string.Empty,
        ["costCentre"] = CostCentreCode ?? string.Empty,
    });

    /// <summary>Reads a request from a query string; without dates it is the current month.</summary>
    public static ReportRequest FromQuery(IQueryCollection query, DateOnly today)
    {
        static DateOnly? Date(string? value) =>
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        var period = Date(query["from"]) is { } from && Date(query["to"]) is { } to && to >= from
            ? ReportPeriod.Days(from, to)
            : ReportPeriod.Month(today.Year, today.Month);
        string? box = query["box"];
        string? costCentre = query["costCentre"];
        return new ReportRequest(period, string.IsNullOrWhiteSpace(box) ? null : box, string.IsNullOrWhiteSpace(costCentre) ? null : costCentre);
    }
}
