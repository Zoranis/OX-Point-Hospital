using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting.WindowsServices;
using OxBox.BuSimulator;

// A Windows service starts in System32, so its settings and pages are taken from the exe's folder.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
});
builder.Services.AddWindowsService(service => service.ServiceName = "OxBoxSimulator");

var options = builder.Configuration.GetSection(SimulatorOptions.SectionName).Get<SimulatorOptions>() ?? new SimulatorOptions();
builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection(SimulatorOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SimulatedBu>();
builder.Services.AddSingleton<BuClients>();
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The WebSocket port is what the real BU exposes; the control page gets a port of its own.
builder.WebHost.ConfigureKestrel(kestrel =>
{
    if (options.ListenOnAllInterfaces)
    {
        kestrel.ListenAnyIP(options.WebSocketPort);
        kestrel.ListenAnyIP(options.ControlPort);
    }
    else
    {
        kestrel.ListenLocalhost(options.WebSocketPort);
        kestrel.ListenLocalhost(options.ControlPort);
    }
});

var app = builder.Build();

app.MapWhen(
    context => context.Connection.LocalPort == options.WebSocketPort,
    bu => bu.UseWebSockets().Run(context => context.RequestServices.GetRequiredService<BuClients>().HandleAsync(context)));

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControlApi();

StartupLog.Listening(app.Logger, options.WebSocketPort, options.ControlPort);

await app.RunAsync();

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "BU simulator: WebSocket on port {WebSocketPort}, control page on port {ControlPort}")]
    public static partial void Listening(ILogger logger, int webSocketPort, int controlPort);
}
