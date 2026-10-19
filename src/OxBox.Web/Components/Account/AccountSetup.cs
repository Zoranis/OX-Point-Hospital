using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using OxBox.Application.Configuration;
using OxBox.Infrastructure.Identity;
using OxBox.Infrastructure.Persistence;

namespace OxBox.Web.Components.Account;

/// <summary>
/// Sign-in for the admin web app, using ASP.NET Core Identity stored in the OxBox database.
/// There is no self-registration: administrators create accounts on the Users page.
/// </summary>
public static class AccountSetup
{
    public static IServiceCollection AddAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<OxBoxDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddValidatedOptions<AccountOptions>(configuration, AccountOptions.SectionName);
        services.AddScoped<AccountSeeder>();
        return services;
    }
}
