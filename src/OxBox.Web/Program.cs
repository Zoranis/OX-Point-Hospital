using Microsoft.EntityFrameworkCore;
using OxBox.Application;
using OxBox.Infrastructure;
using OxBox.Infrastructure.Persistence;
using OxBox.Web.Admin;
using OxBox.Web.Components;
using OxBox.Web.Components.Account;
using OxBox.Web.Kiosk;

var builder = WebApplication.CreateBuilder(args);

// Business logic, persistence and hardware.
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBoxAgents();
builder.Services.AddAlertDelivery();

// User interfaces: the box display and the admin web app.
builder.Services.AddKiosk();
builder.Services.AddAccounts(builder.Configuration);
builder.Services.AddSingleton<SiteClock>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
    await scope.ServiceProvider.GetRequiredService<AccountSeeder>().SeedAsync();

    var settings = await scope.ServiceProvider.GetRequiredService<OxBoxDbContext>().SystemSettings.AsNoTracking().SingleAsync();
    app.Services.GetRequiredService<SiteClock>().UseZone(settings.DisplayTimeZone);
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapAdminEndpoints();

await app.RunAsync();
