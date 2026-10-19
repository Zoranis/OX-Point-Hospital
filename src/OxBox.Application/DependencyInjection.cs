using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OxBox.Application.Alerts;
using OxBox.Application.Boxes;
using OxBox.Application.Configuration;
using OxBox.Application.Exchange;
using OxBox.Application.Inventory;
using OxBox.Application.Service;

namespace OxBox.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<BoxOptions>(configuration, BoxOptions.SectionName);
        services.AddValidatedOptions<ServiceOptions>(configuration, ServiceOptions.SectionName);

        // Shared by the box agents and the web app, for the life of the server.
        services.AddSingleton<BoxStatusBoard>();

        // Scoped: one per handled box event, sharing that event's unit of work.
        services.AddScoped<BoxEvents>();
        services.AddScoped<BoxAlerts>();
        services.AddScoped<StockAlertEvaluator>();
        services.AddScoped<CylinderLedger>();
        services.AddScoped<ExchangeWorkflow>();
        services.AddScoped<ServiceWorkflow>();

        return services;
    }
}
