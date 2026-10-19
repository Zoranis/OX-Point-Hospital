using OxBox.Application.Abstractions;
using OxBox.Application.Feedback;

namespace OxBox.Integration.Tests.Boxes;

/// <summary>A box display that remembers every screen it was told to show.</summary>
internal sealed class RecordingDisplay : IKioskDisplay
{
    private readonly List<KioskScreen> _shown = [];

    public KioskScreen? Last => _shown.Count > 0 ? _shown[^1] : null;

    public IReadOnlyList<KioskScreen> Shown => _shown;

    public void Show(string boxCode, KioskScreen screen) => _shown.Add(screen);
}
