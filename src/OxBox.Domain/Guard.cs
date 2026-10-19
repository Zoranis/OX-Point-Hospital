namespace OxBox.Domain;

internal static class Guard
{
    public static string NotBlank(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
