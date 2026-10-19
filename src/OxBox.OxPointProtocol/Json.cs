using System.Text.Json;

namespace OxBox.OxPointProtocol;

/// <summary>Small, non-throwing readers shared by the protocol parsers.</summary>
internal static class Json
{
    public static string? StringOf(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    public static bool TryGetInt(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }

    public static bool TryGetInt(JsonElement root, string name, out int value)
    {
        value = 0;
        return root.TryGetProperty(name, out var element) && TryGetInt(element, out value);
    }
}
