using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Application.Boxes;
using OxBox.Application.Exchange;
using OxBox.Application.Service;
using OxBox.OxPointProtocol.Commands;

namespace OxBox.Application.Feedback;

/// <summary>
/// User feedback at one box: the status display says what happened and what to do next, and the
/// lamps give an instant signal (green for success, red with the buzzer for an error).
/// </summary>
public sealed partial class BoxSignals(string boxCode, BoxHardware hardware, IKioskDisplay display, ILogger<BoxSignals> logger)
{
    // How the lamps are wired to the BU relay shield. The relay names come from the OX Point
    // document, which was written for locker heating; here they drive lamps.
    internal const ShieldRelay GreenLamp = ShieldRelay.Heating;
    internal const ShieldRelay RedLampAndBuzzer = ShieldRelay.Cooling;

    public void Show(KioskScreen screen) => display.Show(boxCode, screen);

    public async Task SucceedAsync(KioskScreen screen, CancellationToken cancellationToken)
    {
        Show(screen);
        await PulseAsync(GreenLamp, cancellationToken);
    }

    public async Task RejectAsync(ExchangeError error, int? returnPositionNumber, CancellationToken cancellationToken)
    {
        Show(new RejectedScreen(error, returnPositionNumber));
        await PulseAsync(RedLampAndBuzzer, cancellationToken);
    }

    public async Task RejectServiceAsync(ServiceError error, KioskScreen resume, CancellationToken cancellationToken)
    {
        Show(new ServiceRejectedScreen(error, resume));
        await PulseAsync(RedLampAndBuzzer, cancellationToken);
    }

    private async Task PulseAsync(ShieldRelay relay, CancellationToken cancellationToken)
    {
        if (!await hardware.PulseAsync(relay, cancellationToken))
        {
            // The display still informs the user; a missing lamp is a maintenance issue.
            LogLampNotAcknowledged(boxCode, relay);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Box {BoxCode}: relay {Relay} was not acknowledged; is the relay shield fitted?")]
    private partial void LogLampNotAcknowledged(string boxCode, ShieldRelay relay);
}
