using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;
using OxBox.Application.Configuration;
using OxBox.Infrastructure.Alerts;
using OxBox.Infrastructure.Boxes;
using OxBox.Infrastructure.OxPoint;
using OxBox.Infrastructure.Persistence;
using OxBox.Infrastructure.Reports;

namespace OxBox.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "OxBox";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        // The factory serves web pages, which live longer than one unit of work; workflows use
        // the scoped context.
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddDbContextFactory<OxBoxDbContext>(
            (provider, options) => options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()),
            ServiceLifetime.Scoped);
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IBoxStore, EfBoxStore>();
        services.AddScoped<ReportQueries>();

        services.AddValidatedOptions<DatabaseOptions>(configuration, DatabaseOptions.SectionName);
        services.AddValidatedOptions<BuConnectionOptions>(configuration, BuConnectionOptions.SectionName);
        services.AddValidatedOptions<SmtpOptions>(configuration, SmtpOptions.SectionName);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IBoxGatewayFactory, BuConnectionFactory>();

        return services;
    }

    /// <summary>Runs one agent per box in the background. Leave out where no hardware should be driven.</summary>
    public static IServiceCollection AddBoxAgents(this IServiceCollection services)
    {
        services.AddSingleton<BoxSupervisor>();
        services.AddSingleton<IBoxAgents>(provider => provider.GetRequiredService<BoxSupervisor>());
        services.AddHostedService(provider => provider.GetRequiredService<BoxSupervisor>());
        return services;
    }

    /// <summary>Sends queued alert e-mails and SMS in the background.</summary>
    public static IServiceCollection AddAlertDelivery(this IServiceCollection services)
    {
        services.AddSingleton<IMessageSender, SmtpEmailSender>();
        services.AddSingleton<IMessageSender, LoggingSmsSender>();
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
