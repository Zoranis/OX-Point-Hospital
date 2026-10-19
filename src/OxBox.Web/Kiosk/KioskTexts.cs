using System.Resources;

namespace OxBox.Web.Kiosk;

/// <summary>
/// The kiosk's wording, from <c>Resources/Kiosk/KioskTexts.resx</c> (Czech) and
/// <c>KioskTexts.en.resx</c> (English). The language is always passed explicitly, because a
/// display switches language per user rather than per browser.
/// </summary>
public static class KioskTexts
{
    private static readonly ResourceManager Resources =
        new("OxBox.Web.Resources.Kiosk.KioskTexts", typeof(KioskTexts).Assembly);

    public static string Get(string key, KioskLanguage language) =>
        Resources.GetString(key, language.Culture())
        ?? throw new KeyNotFoundException($"Kiosk text '{key}' is missing.");

    public static string Format(string key, KioskLanguage language, params object[] arguments) =>
        string.Format(language.Culture(), Get(key, language), arguments);
}
