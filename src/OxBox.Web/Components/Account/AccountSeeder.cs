using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OxBox.Infrastructure.Identity;
using OxBox.Web.Admin;

namespace OxBox.Web.Components.Account;

/// <summary>
/// Makes sure the roles exist and, on a database without users, creates the configured first
/// accounts. Idempotent, so it runs on every start.
/// </summary>
public sealed partial class AccountSeeder(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    IOptions<AccountOptions> options,
    ILogger<AccountSeeder> logger)
{
    public async Task SeedAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }
        }

        if (users.Users.Any())
        {
            return;
        }

        if (options.Value.InitialAdmin is { } admin)
        {
            await CreateAsync(admin, Roles.Admin);
        }
        else
        {
            LogNoInitialAdmin();
        }

        foreach (var viewer in options.Value.DemoViewers)
        {
            await CreateAsync(viewer, Roles.Viewer);
        }
    }

    private async Task CreateAsync(SeedAccount account, string role)
    {
        var user = new ApplicationUser { UserName = account.Email, Email = account.Email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, account.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create account {account.Email}: {string.Join(" ", result.Errors.Select(error => error.Description))}");
        }

        await users.AddToRoleAsync(user, role);
        foreach (var costCentre in account.CostCentres)
        {
            await users.AddClaimAsync(user, new Claim(Roles.CostCentreClaim, costCentre));
        }

        LogCreated(account.Email, role);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created {Role} account {Email}")]
    private partial void LogCreated(string email, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No users exist and Accounts:InitialAdmin is not set; nobody can sign in to the web app")]
    private partial void LogNoInitialAdmin();
}
