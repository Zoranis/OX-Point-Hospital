namespace OxBox.Web.Components.Account;

/// <summary>
/// Accounts created on first start, so a fresh installation can be signed in to. Production sets
/// only the initial administrator, through environment variables or user secrets, and changes its
/// password after the first sign-in.
/// </summary>
public sealed class AccountOptions
{
    public const string SectionName = "Accounts";

    /// <summary>Created when the database has no users at all.</summary>
    public SeedAccount? InitialAdmin { get; set; }

    /// <summary>Demo only: read-only users, each limited to the listed cost centres.</summary>
    public List<SeedAccount> DemoViewers { get; set; } = [];
}
