using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using OxBox.Application;
using OxBox.Infrastructure;
using OxBox.Infrastructure.Persistence;
using OxBox.Web.Admin;
using OxBox.Web.Components;
using OxBox.Web.Components.Account;
using OxBox.Web.Kiosk;

// A Windows service starts in System32, so its settings and data are taken from the exe's folder.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});
builder.Services.AddWindowsService(service => service.ServiceName = "OxBox");

// Business logic, persistence and hardware.
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBoxAgents();
builder.Services.AddAlertDelivery();

// User interfaces: the box display and the admin web app.
builder.Services.AddKiosk();
builder.Services.AddAccounts(builder.Configuration);
// The sign-in cookie keys live next to the database, so sign-ins survive a restart and a backup
// of the data folder holds everything. On Windows they are encrypted for this machine.
var dataProtection = builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "data", "keys")));
if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}
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
app.MapKioskEndpoints();

await app.RunAsync();
