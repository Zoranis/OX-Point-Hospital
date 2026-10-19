namespace OxBox.Web.Admin;

/// <summary>
/// The web app's roles. An administrator manages everything; a viewer sees, read-only, the data
/// of the cost centres listed in their <see cref="CostCentreClaim"/> claims (RFQ: "administrator
/// vs. read-only user for individual cost centres").
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";

    public const string Viewer = "Viewer";

    /// <summary>One claim per cost centre code a viewer may see.</summary>
    public const string CostCentreClaim = "oxbox:cost-centre";

    public static readonly IReadOnlyList<string> All = [Admin, Viewer];
}
