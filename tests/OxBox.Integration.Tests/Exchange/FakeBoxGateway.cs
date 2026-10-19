using System.Threading.Channels;
using OxBox.Application.Abstractions;
using OxBox.OxPointProtocol;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.Integration.Tests.Exchange;

/// <summary>
/// An in-memory BU for workflow tests. Tests jam locks or disconnect it to drive each
/// failure path.
/// </summary>
internal sealed class FakeBoxGateway : IBoxGateway
{
    private readonly HashSet<LockAddress> _jammedLocks = [];
    private readonly List<BuCommand> _sent = [];

    public ChannelReader<GatewayEvent> Events { get; } = Channel.CreateUnbounded<GatewayEvent>().Reader;

    public bool IsConnected { get; set; } = true;

    public IReadOnlyList<LockAddress> OpenedLocks => _sent.OfType<OpenLock>().Select(open => open.Lock).ToList();

    public IReadOnlyList<ShieldRelay> PulsedRelays => _sent.OfType<PulseRelay>().Select(pulse => pulse.Relay).ToList();

    /// <summary>The lock never answers, so opening it times out.</summary>
    public void Jam(LockAddress address) => _jammedLocks.Add(address);

    public Task<CommandResult> SendAsync(BuCommand command, CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            return Task.FromResult(CommandResult.NotConnected);
        }

        _sent.Add(command);
        return Task.FromResult(command switch
        {
            OpenLock open when _jammedLocks.Contains(open.Lock) => CommandResult.TimedOut,
            OpenLock open => CommandResult.Replied(new LockOpenReply(open.Lock, Succeeded: true, "OK")),
            PulseRelay => CommandResult.Replied(new RelayAcknowledged()),
            _ => CommandResult.SentWithoutReply,
        });
    }

    public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
