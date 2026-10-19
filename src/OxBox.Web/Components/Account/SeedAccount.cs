namespace OxBox.Web.Components.Account;

/// <summary>An account to create on first start (see <see cref="AccountOptions"/>).</summary>
public sealed class SeedAccount
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> CostCentres { get; set; } = [];
}
