namespace OxBox.OxPointProtocol;

/// <summary>
/// Hardware limits from the OX Point technical document (sections 2–4).
/// </summary>
public static class OxPointLimits
{
    /// <summary>The BU's WebSocket server port unless reconfigured over USB.</summary>
    public const int DefaultPort = 3030;

    /// <summary>CUs are chained behind a BU and indexed from zero.</summary>
    public const int MinBoardIndex = 0;

    public const int MaxBoardIndex = 9;

    /// <summary>Locks on a CU are numbered from one.</summary>
    public const int MinLockNumber = 1;

    public const int MaxLockNumber = 16;

    public static bool IsValidBoard(int board) => board is >= MinBoardIndex and <= MaxBoardIndex;

    public static bool IsValidLock(int lockNumber) => lockNumber is >= MinLockNumber and <= MaxLockNumber;
}
