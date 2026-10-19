using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace OxBox.Web.Kiosk;

public static class KioskLanguages
{
    /// <summary>The query parameter that sets a display's default language, e.g. <c>?lang=en</c>.</summary>
    private const string QueryParameter = "lang";

    public static CultureInfo Culture(this KioskLanguage language) => CultureInfo.GetCultureInfo(language.Code());

    public static string Code(this KioskLanguage language) => language switch
    {
        KioskLanguage.Czech => "cs",
        KioskLanguage.English => "en",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
    };

    /// <summary>The short label on the language buttons.</summary>
    public static string Label(this KioskLanguage language) => language switch
    {
        KioskLanguage.Czech => "CZ",
        KioskLanguage.English => "EN",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language."),
    };

    /// <summary>The language with this code (<c>cs</c>, <c>en</c>), or <c>null</c> if there is none.</summary>
    public static KioskLanguage? FromCode(string? code) =>
        Enum.GetValues<KioskLanguage>()
            .Select(language => (KioskLanguage?)language)
            .FirstOrDefault(language => string.Equals(language!.Value.Code(), code, StringComparison.OrdinalIgnoreCase));

    /// <summary>The display's default language from its URL. Czech when absent or not recognised.</summary>
    public static KioskLanguage DefaultFromUrl(string url)
    {
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        return FromCode(query.TryGetValue(QueryParameter, out var code) ? code.ToString() : null) ?? KioskLanguage.Czech;
    }
}
