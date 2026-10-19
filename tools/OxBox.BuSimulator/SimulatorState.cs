namespace OxBox.BuSimulator;

/// <summary>Everything the control page shows, taken in one consistent snapshot.</summary>
public sealed record SimulatorState(
    int ConnectedClients,
    IReadOnlyList<LockState> Locks,
    bool GreenLampOn,
    bool RedLampAndBuzzerOn,
    string? KeypadLed,
    IReadOnlyList<TrafficEntry> Traffic,
    IReadOnlyList<string> QuickScanCodes);

public sealed record LockState(int Board, int Lock, bool IsOpen, bool IsJammed);

public sealed record TrafficEntry(DateTimeOffset At, TrafficDirection Direction, string Json);

public enum TrafficDirection
{
    /// <summary>From a client (the OxBox server) to the BU.</summary>
    In,

    /// <summary>From the BU to a client.</summary>
    Out,
}
