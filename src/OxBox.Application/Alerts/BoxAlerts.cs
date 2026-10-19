using Microsoft.Extensions.Logging;
using OxBox.Application.Abstractions;
using OxBox.Domain.Alerts;

namespace OxBox.Application.Alerts;

/// <summary>
/// Raises and resolves alerts. A raised alert and its e-mails and SMS are added to the caller's
/// unit of work, so they are saved together with the change that caused them, and sent later by
/// the outbox dispatcher. A condition that is already active is not raised again.
/// </summary>
public sealed partial class BoxAlerts(IBoxStore store, TimeProvider time, ILogger<BoxAlerts> logger)
{
    // Alerts raised in this unit of work and not saved yet, which the store cannot see.
    private readonly Dictionary<string, Alert> _raisedHere = [];

    /// <returns>True if a new alert was raised; false if the condition was already active.</returns>
    public async Task<bool> RaiseAsync(AlertFacts facts, CancellationToken cancellationToken)
    {
        var key = facts.Key;
        if (_raisedHere.ContainsKey(key) || await store.FindActiveAlertAsync(key, cancellationToken) is not null)
        {
            return false;
        }

        var now = time.GetUtcNow();
        var alert = new Alert(facts.Kind, key, facts.BoxCode, facts.CostCentre.Id, AlertMessages.EnglishDetails(facts), now);
        store.Add(alert);
        _raisedHere[key] = alert;

        var settings = await store.GetSettingsAsync(cancellationToken);
        var subject = AlertMessages.Subject(facts);
        var body = AlertMessages.Body(facts, now, settings.DisplayTimeZone);
        var recipients = await store.GetAlertRecipientsAsync(facts.CostCentre.Id, cancellationToken);
        foreach (var recipient in recipients)
        {
            if (recipient.Email is { } email)
            {
                store.Add(new OutboxMessage(alert, MessageChannel.Email, email, subject, body, now));
            }

            if (recipient.Phone is { } phone)
            {
                store.Add(new OutboxMessage(alert, MessageChannel.Sms, phone, subject, AlertMessages.Sms(facts), now));
            }
        }

        LogRaised(key, recipients.Count);
        return true;
    }

    /// <summary>The condition is over; raising it again later sends new messages.</summary>
    public async Task ResolveAsync(AlertFacts facts, CancellationToken cancellationToken)
    {
        var key = facts.Key;
        var alert = _raisedHere.GetValueOrDefault(key) ?? await store.FindActiveAlertAsync(key, cancellationToken);
        if (alert is { IsActive: true })
        {
            alert.Resolve(time.GetUtcNow());
            _raisedHere.Remove(key);
            LogResolved(key);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Alert {Key} raised for {Recipients} recipient(s)")]
    private partial void LogRaised(string key, int recipients);

    [LoggerMessage(Level = LogLevel.Information, Message = "Alert {Key} resolved")]
    private partial void LogResolved(string key);
}
