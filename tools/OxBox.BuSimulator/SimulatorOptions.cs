using OxBox.OxPointProtocol;

namespace OxBox.BuSimulator;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>Port of the BU WebSocket server, as on the real device.</summary>
    public int WebSocketPort { get; set; } = OxPointLimits.DefaultPort;

    /// <summary>Port of the control page and its API.</summary>
    public int ControlPort { get; set; } = 5080;

    /// <summary>
    /// Listen on every network interface instead of only localhost. For a container, where OxBox
    /// and the HTTPS proxy reach the simulator over a private network. The simulator has no
    /// sign-in, so never expose its ports directly.
    /// </summary>
    public bool ListenOnAllInterfaces { get; set; }

    /// <summary>
    /// Wired locks per CU, in board order. The RFQ box has 30 positions: 16 on CU0 and 14 on CU1.
    /// </summary>
    /// <remarks>
    /// Defaults to empty on purpose: configuration binding appends to an array's existing items,
    /// so a non-empty default would be doubled. <see cref="EffectiveWiring"/> supplies the fallback.
    /// </remarks>
    public int[] WiredLocksPerBoard { get; set; } = [];

    public IReadOnlyList<int> EffectiveWiring => WiredLocksPerBoard is { Length: > 0 } wiring ? wiring : [16, 14];

    /// <summary>How long a relay stays on after <c>OpenRelay</c>; set on the real BU over USB.</summary>
    public TimeSpan RelayPulse { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>When false, <c>OpenRelay</c> gets no reply, as on a BU without the relay shield.</summary>
    public bool RelayShieldFitted { get; set; } = true;

    /// <summary>Barcodes offered as one-click buttons on the control page.</summary>
    public string[] QuickScanCodes { get; set; } = [];
}
