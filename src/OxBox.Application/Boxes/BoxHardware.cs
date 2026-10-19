using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Domain.Boxes;
using OxBox.OxPointProtocol.Commands;
using OxBox.OxPointProtocol.Messages;

namespace OxBox.Application.Boxes;

/// <summary>
/// The box's hardware in the terms the workflows think in: open this position, light this
/// lamp. Retries and protocol details stay here, out of the workflows.
/// </summary>
public sealed partial class BoxHardware(string boxCode, IBoxGateway gateway, int openAttempts, ILogger<BoxHardware> logger)
{
    public async Task<OpenOutcome> OpenAsync(Position position, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= openAttempts; attempt++)
        {
            var result = await gateway.SendAsync(new OpenLock(position.Lock), cancellationToken);
            switch (result)
            {
                case { Outcome: CommandOutcome.NotConnected }:
                    return OpenOutcome.NotConnected;
                case { Reply: LockOpenReply { Succeeded: true } }:
                    return OpenOutcome.Opened;
                default:
                    LogOpenAttemptFailed(boxCode, position.Number, attempt, result.Outcome);
                    break;
            }
        }

        return OpenOutcome.Failed;
    }

    /// <returns>True if the BU confirmed the pulse; false means no relay shield or no connection.</returns>
    public async Task<bool> PulseAsync(ShieldRelay relay, CancellationToken cancellationToken)
    {
        var result = await gateway.SendAsync(new PulseRelay(relay), cancellationToken);
        return result.Outcome == CommandOutcome.Replied;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Box {BoxCode}: opening position {Position} failed on attempt {Attempt} ({Outcome})")]
    private partial void LogOpenAttemptFailed(string boxCode, int position, int attempt, CommandOutcome outcome);
}
