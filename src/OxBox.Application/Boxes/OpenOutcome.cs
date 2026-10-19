namespace OxBox.Application.Boxes;

public enum OpenOutcome
{
    /// <summary>The BU confirmed the lock opened.</summary>
    Opened,

    /// <summary>The BU is unreachable; try again once it is back.</summary>
    NotConnected,

    /// <summary>The BU is reachable but the lock did not confirm, even after retrying.</summary>
    Failed,
}
