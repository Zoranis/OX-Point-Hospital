using System.Threading.Channels;
using OxBox.OxPointProtocol.Commands;

namespace OxBox.Application.Abstractions;

/// <summary>
/// The connection to one box's BU. Business logic talks to the hardware only through this port.
/// </summary>
public interface IBoxGateway : IAsyncDisposable
{
    /// <summary>
    /// Unsolicited BU messages (scans, INFO notifications) and connection changes, in arrival order.
    /// </summary>
    ChannelReader<GatewayEvent> Events { get; }

    /// <summary>
    /// Keeps the connection open, reconnecting with backoff, until <paramref name="cancellationToken"/> fires.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends one command and waits for its reply. Commands are sent one at a time, because BU
    /// replies carry no request ID and can only be matched by content.
    /// </summary>
    Task<CommandResult> SendAsync(BuCommand command, CancellationToken cancellationToken);
}
