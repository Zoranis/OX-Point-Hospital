using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using OxBox.Domain.Transactions;

namespace OxBox.Web.Admin;

/// <summary>
/// The transaction history filter, shared by the Transactions page and its CSV export so both
/// always list exactly the same rows. Dates are local days in the display time zone.
/// </summary>
public sealed record TransactionFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    string? BoxCode = null,
    string? CostCentreCode = null,
    CylinderAction? Action = null,
    string? Barcode = null)
{
    public IQueryable<CylinderTransaction> Apply(IQueryable<CylinderTransaction> query, SiteClock clock, UserScope scope)
    {
        if (scope.AllowedCostCentres is { } allowed)
        {
            query = query.Where(t => allowed.Contains(t.CostCentreCode));
        }

        if (From is { } from)
        {
            var fromUtc = clock.StartOfDayUtc(from);
            query = query.Where(t => t.AtUtc >= fromUtc);
        }

        if (To is { } to)
        {
            var toUtc = clock.StartOfDayUtc(to.AddDays(1));
            query = query.Where(t => t.AtUtc < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(BoxCode))
        {
            query = query.Where(t => t.BoxCode == BoxCode);
        }

        if (!string.IsNullOrWhiteSpace(CostCentreCode))
        {
            query = query.Where(t => t.CostCentreCode == CostCentreCode);
        }

        if (Action is { } action)
        {
            query = query.Where(t => t.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(Barcode))
        {
            var barcode = Barcode.Trim();
            query = query.Where(t => t.Barcode.Contains(barcode));
        }

        return query.OrderByDescending(t => t.AtUtc).ThenByDescending(t => t.Id);
    }

    public string ToQueryString()
    {
        var values = new Dictionary<string, string?>
        {
            ["from"] = From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["to"] = To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["box"] = BoxCode,
            ["costCentre"] = CostCentreCode,
            ["action"] = Action?.ToString(),
            ["barcode"] = Barcode,
        };
        return QueryHelpers.AddQueryString(string.Empty, values.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)));
    }

    public static TransactionFilter FromQuery(IQueryCollection query)
    {
        static DateOnly? Date(string? value) =>
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        return new TransactionFilter(
            Date(query["from"]),
            Date(query["to"]),
            NullIfEmpty(query["box"]),
            NullIfEmpty(query["costCentre"]),
            Enum.TryParse<CylinderAction>(query["action"], out var action) ? action : null,
            NullIfEmpty(query["barcode"]));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
