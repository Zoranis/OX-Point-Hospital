using Microsoft.Extensions.Logging;
using OxBox.Domain.Alerts;

namespace OxBox.Infrastructure.Alerts;

/// <summary>
/// Stands in for an SMS gateway until the hospital chooses one (open question #5). The message is
/// written to the log, and the outbox records that it was logged rather than sent.
/// </summary>
internal sealed partial class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : IMessageSender
{
    public MessageChannel Channel => MessageChannel.Sms;

    public Task<string?> SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        LogSms(message.Recipient, message.Body);
        return Task.FromResult<string?>("Logged only: no SMS provider is configured yet.");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMS to {Recipient} (no SMS provider configured, logged only): {Text}")]
    private partial void LogSms(string recipient, string text);
}
