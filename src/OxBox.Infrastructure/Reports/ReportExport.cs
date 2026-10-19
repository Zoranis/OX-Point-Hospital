using System.Globalization;
using System.Text;

namespace OxBox.Infrastructure.Reports;

/// <summary>
/// Writes report tables as CSV. The CSV uses semicolons and a UTF-8 byte-order mark,
/// which is what Excel with Czech regional settings opens correctly.
/// </summary>
public static class ReportExport
{
    public static byte[] Csv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object>> rows)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join(';', headers.Select(Escape)));
        foreach (var row in rows)
        {
            text.AppendLine(string.Join(';', row.Select(cell => Escape(Format(cell)))));
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static string Format(object cell) => cell switch
    {
        DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => cell.ToString() ?? string.Empty,
    };

    private static string Escape(string value) =>
        value.AsSpan().IndexOfAny(";\"\n\r") >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
