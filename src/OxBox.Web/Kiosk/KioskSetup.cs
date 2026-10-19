using OxBox.Application.Abstractions;

namespace OxBox.Web.Kiosk;

public static class KioskSetup
{
    /// <summary>The in-memory screens that the box agents publish to and the kiosk pages show.</summary>
    public static IServiceCollection AddKiosk(this IServiceCollection services)
    {
        services.AddSingleton<KioskScreens>();
        services.AddSingleton<IKioskDisplay>(provider => provider.GetRequiredService<KioskScreens>());
        return services;
    }
}
