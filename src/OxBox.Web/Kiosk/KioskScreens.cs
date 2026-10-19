using System.Collections.Concurrent;
using OxBox.Application.Abstractions;
using OxBox.Application.Feedback;

namespace OxBox.Web.Kiosk;

/// <summary>
/// Holds the latest screen of every box's display and tells open kiosk pages when it changes.
/// In memory only: after a restart each box agent shows its current screen again.
/// </summary>
public sealed class KioskScreens(TimeProvider time) : IKioskDisplay
{
    /// <summary>How long a thank-you or error stays up before the display moves on (see <see cref="KioskScreen.ThenShow"/>).</summary>
    private static readonly TimeSpan TransientScreenDuration = TimeSpan.FromSeconds(8);

    private readonly ConcurrentDictionary<string, ShownScreen> _screens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised with the box code whenever that box's screen changes.</summary>
    public event Action<string>? Changed;

    public void Show(string boxCode, KioskScreen screen)
    {
        _screens[boxCode] = new ShownScreen(screen, time.GetUtcNow());
        Changed?.Invoke(boxCode);
    }

    /// <summary>The screen to show now. A box that has never reported is out of service.</summary>
    public KioskScreen Current(string boxCode)
    {
        if (!_screens.TryGetValue(boxCode, out var shown))
        {
            return new OutOfServiceScreen();
        }

        var expired = shown.Screen.IsTransient && time.GetUtcNow() - shown.ShownAtUtc > TransientScreenDuration;
        return expired ? shown.Screen.ThenShow : shown.Screen;
    }

    private sealed record ShownScreen(KioskScreen Screen, DateTimeOffset ShownAtUtc);
}
