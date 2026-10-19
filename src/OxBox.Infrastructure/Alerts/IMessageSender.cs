using OxBox.Domain.Alerts;

namespace OxBox.Infrastructure.Alerts;

/// <summary>Delivers one alert message over one channel. Throws if the message could not be delivered.</summary>
internal interface IMessageSender
{
    MessageChannel Channel { get; }

    /// <returns>A note to keep with the message, for example when it was only logged; otherwise <c>null</c>.</returns>
    Task<string?> SendAsync(OutboxMessage message, CancellationToken cancellationToken);
}
