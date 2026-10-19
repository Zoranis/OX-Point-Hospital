using Microsoft.AspNetCore.Identity;

namespace OxBox.Infrastructure.Identity;

/// <summary>
/// A web app user. The role (Admin or Viewer) and, for viewers, the cost centres they may see are
/// stored as Identity roles and claims, so a later switch to hospital sign-in (Entra ID) only
/// changes where they come from.
/// </summary>
public sealed class ApplicationUser : IdentityUser;
