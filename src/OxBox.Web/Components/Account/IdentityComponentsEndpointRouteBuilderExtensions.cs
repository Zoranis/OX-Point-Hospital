using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OxBox.Infrastructure.Identity;

namespace Microsoft.AspNetCore.Routing;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // Sign-out needs a POST endpoint; signing in is handled by the Login page itself.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        return accountGroup;
    }
}
