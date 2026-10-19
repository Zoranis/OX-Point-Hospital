using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OxBox.Application.Configuration;

public static class OptionsRegistration
{
    /// <summary>
    /// Binds a settings section and validates its data annotations when the app starts, so a bad
    /// setting stops startup instead of failing later. Defaults live in the options class itself.
    /// </summary>
    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
