using System.Globalization;
using OxBox.Domain.Alerts;

namespace OxBox.Application.Alerts;

/// <summary>
/// The wording of alert e-mails and SMS. Recipients are hospital staff, so Czech comes first and
/// English follows for anyone who does not read Czech.
/// </summary>
public static class AlertMessages
{
    private const int SmsMaxLength = 300;

    public static string EnglishSubject(AlertFacts facts) => Describe(facts, english: true).Subject;

    public static string EnglishDetails(AlertFacts facts) => Describe(facts, english: true).Body;

    public static string Subject(AlertFacts facts) => $"OxBox: {Describe(facts, english: false).Subject}";

    public static string Body(AlertFacts facts, DateTimeOffset raisedAtUtc, TimeZoneInfo timeZone)
    {
        var czech = Describe(facts, english: false);
        var english = Describe(facts, english: true);
        var local = TimeZoneInfo.ConvertTime(raisedAtUtc, timeZone).ToString("d. M. yyyy HH:mm", CultureInfo.InvariantCulture);

        return $"""
            {czech.Subject}

            {czech.Body}
            Čas: {local}

            — English —
            {english.Subject}

            {english.Body}
            Time: {local}
            """;
    }

    public static string Sms(AlertFacts facts)
    {
        var czech = Describe(facts, english: false);
        var text = $"OxBox: {czech.Subject}. {czech.Body}";
        return text.Length <= SmsMaxLength ? text : text[..(SmsMaxLength - 1)] + "…";
    }

    private static (string Subject, string Body) Describe(AlertFacts facts, bool english)
    {
        var box = facts.BoxCode;
        var costCentre = $"{facts.CostCentre.Name} ({facts.CostCentre.Code})";
        var count = facts.Count.ToString(CultureInfo.InvariantCulture);

        return (facts.Kind, english) switch
        {
            (AlertKind.LowStock, false) => (
                $"Docházejí plné láhve – {costCentre}",
                $"Středisko {costCentre} má v boxu {box} už jen {count} plné láhve. Doplňte prosím box."),
            (AlertKind.LowStock, true) => (
                $"Low stock – {costCentre}",
                $"Cost centre {costCentre} has only {count} full cylinders left in box {box}. Please restock the box."),
            (AlertKind.LastFullCylinder, false) => (
                $"Poslední plná láhev – {costCentre}",
                $"Středisko {costCentre} má v boxu {box} poslední plnou láhev. Doplňte box co nejdříve."),
            (AlertKind.LastFullCylinder, true) => (
                $"Last full cylinder – {costCentre}",
                $"Cost centre {costCentre} has its last full cylinder in box {box}. Restock the box as soon as possible."),
            (AlertKind.NoFullCylinder, false) => (
                $"Žádná plná láhev – {costCentre}",
                $"Středisko {costCentre} nemá v boxu {box} žádnou plnou láhev. Dodávka kyslíku pro toto středisko je přerušena. Doplňte box ihned."),
            (AlertKind.NoFullCylinder, true) => (
                $"No full cylinder – {costCentre}",
                $"Cost centre {costCentre} has no full cylinder left in box {box}. Its oxygen supply is interrupted. Restock the box now."),
            _ => throw new ArgumentOutOfRangeException(nameof(facts), facts.Kind, "Unknown alert kind."),
        };
    }
}
