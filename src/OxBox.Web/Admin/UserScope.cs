using System.Security.Claims;

namespace OxBox.Web.Admin;

/// <summary>What the signed-in user may see and do.</summary>
public sealed record UserScope(string UserName, bool IsAdmin, IReadOnlyCollection<string>? AllowedCostCentres)
{
    public static UserScope From(ClaimsPrincipal user)
    {
        var isAdmin = user.IsInRole(Roles.Admin);
        var costCentres = user.FindAll(Roles.CostCentreClaim).Select(claim => claim.Value).ToList();
        return new UserScope(user.Identity?.Name ?? "unknown", isAdmin, isAdmin ? null : costCentres);
    }

    /// <summary>Whether data of this cost centre may be shown.</summary>
    public bool MaySee(string costCentreCode) => AllowedCostCentres is null || AllowedCostCentres.Contains(costCentreCode);
}
