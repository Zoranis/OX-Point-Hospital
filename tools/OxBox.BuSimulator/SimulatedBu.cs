using Microsoft.Extensions.Options;
using OxBox.OxPointProtocol;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.BuSimulator;

/// <summary>
/// The simulated hardware: locks with doors, the two shield relays (green lamp, red lamp + buzzer),
/// and the keypad LED. Pure state, with no networking. Methods return the
/// BU messages to send, and <see cref="BuClients"/> delivers them.
/// </summary>
public sealed class SimulatedBu
{
    private const int MaxTrafficEntries = 40;

    private readonly Lock _gate = new();
    private readonly SimulatorOptions _options;
    private readonly TimeProvider _time;
    private readonly SortedDictionary<LockAddress, SimulatedLock> _locks = new(LockAddressComparer.Instance);
    private readonly DateTimeOffset[] _relayOnUntil = new DateTimeOffset[Enum.GetValues<ShieldRelay>().Length];
    private readonly LinkedList<TrafficEntry> _traffic = new();
    private (bool GoodRead, DateTimeOffset Until)? _keypadLed;

    public SimulatedBu(IOptions<SimulatorOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;

        for (var board = 0; board < _options.EffectiveWiring.Count; board++)
        {
            for (var lockNumber = 1; lockNumber <= _options.EffectiveWiring[board]; lockNumber++)
            {
                _locks[new LockAddress(board, lockNumber)] = new SimulatedLock();
            }
        }
    }

    /// <summary>Applies a command from a client and returns the reply, or <c>null</c> for no reply.</summary>
    public BuMessage? Handle(BuCommand command)
    {
        lock (_gate)
        {
            return command switch
            {
                OpenLock open => OpenLock(open.Lock),
                RequestLockStatus request => new LockStatusReply(request.Board, MaskOf(request.Board)),
                SendKeypadStatus status => ShowKeypadLed(status.GoodRead),
                PulseRelay pulse => PulseRelay(pulse.Relay),
                RequestDoorContact => new DoorContactReply(true), // The box door (TAMPER) is not simulated: always closed.
                _ => null,
            };
        }
    }

    public ScanReceived Scan(string code) => new(code.Trim(), ScanSource.QrOrBarcode);

    /// <summary>Someone shuts the door of an open compartment.</summary>
    public bool CloseDoor(LockAddress address)
    {
        lock (_gate)
        {
            if (!_locks.TryGetValue(address, out var simulatedLock))
            {
                return false;
            }

            simulatedLock.IsOpen = false;
            return true;
        }
    }

    /// <summary>A jammed lock never answers <c>Open</c>, which lets the fault path be demonstrated.</summary>
    public bool ToggleJam(LockAddress address)
    {
        lock (_gate)
        {
            if (!_locks.TryGetValue(address, out var simulatedLock))
            {
                return false;
            }

            simulatedLock.IsJammed = !simulatedLock.IsJammed;
            return true;
        }
    }

    public void RecordTraffic(TrafficDirection direction, string json)
    {
        lock (_gate)
        {
            _traffic.AddFirst(new TrafficEntry(_time.GetUtcNow(), direction, json));
            if (_traffic.Count > MaxTrafficEntries)
            {
                _traffic.RemoveLast();
            }
        }
    }

    public SimulatorState Snapshot(int connectedClients)
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            var locks = _locks
                .Select(pair => new LockState(pair.Key.Board, pair.Key.LockNumber, pair.Value.IsOpen, pair.Value.IsJammed))
                .ToList();

            // OxBox wiring: relay 0 drives the green lamp, relay 1 the red lamp and buzzer.
            return new SimulatorState(
                connectedClients,
                locks,
                GreenLampOn: _relayOnUntil[(int)ShieldRelay.Heating] > now,
                RedLampAndBuzzerOn: _relayOnUntil[(int)ShieldRelay.Cooling] > now,
                KeypadLed: _keypadLed is { } led && led.Until > now ? (led.GoodRead ? "green" : "red") : null,
                [.. _traffic],
                _options.QuickScanCodes);
        }
    }

    private LockOpenReply? OpenLock(LockAddress address)
    {
        if (!_locks.TryGetValue(address, out var simulatedLock))
        {
            // Nothing is wired there. The real BU cannot tell, so it confirms anyway.
            return new LockOpenReply(address, Succeeded: true, "OK");
        }

        if (simulatedLock.IsJammed)
        {
            return null;
        }

        simulatedLock.IsOpen = true;
        return new LockOpenReply(address, Succeeded: true, "OK");
    }

    // Only wired, shut locks read as closed. Open and unwired locks read as open, as on the real BU.
    private LockMask MaskOf(int board) =>
        LockMask.WithClosedLocks(_locks
            .Where(pair => pair.Key.Board == board && !pair.Value.IsOpen)
            .Select(pair => pair.Key.LockNumber));

    private BuMessage? ShowKeypadLed(bool goodRead)
    {
        _keypadLed = (goodRead, _time.GetUtcNow() + _options.RelayPulse);
        return null;
    }

    private RelayAcknowledged? PulseRelay(ShieldRelay relay)
    {
        if (!_options.RelayShieldFitted)
        {
            return null;
        }

        _relayOnUntil[(int)relay] = _time.GetUtcNow() + _options.RelayPulse;
        return new RelayAcknowledged();
    }

    private sealed class SimulatedLock
    {
        public bool IsOpen { get; set; }

        public bool IsJammed { get; set; }
    }

    private sealed class LockAddressComparer : IComparer<LockAddress>
    {
        public static readonly LockAddressComparer Instance = new();

        public int Compare(LockAddress x, LockAddress y) =>
            x.Board != y.Board ? x.Board.CompareTo(y.Board) : x.LockNumber.CompareTo(y.LockNumber);
    }
}
