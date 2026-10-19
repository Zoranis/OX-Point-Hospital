using OxBox.Application.Feedback;

namespace OxBox.Application.Abstractions;

/// <summary>The status display inside a box. It only shows information; users cannot type on it.</summary>
public interface IKioskDisplay
{
    void Show(string boxCode, KioskScreen screen);
}
