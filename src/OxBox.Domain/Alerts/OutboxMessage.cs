namespace OxBox.Domain.Alerts;

public enum MessageChannel
{
    Email,
    Sms,
}

/// <summary>
/// An e-mail or SMS waiting to be sent. It is written in the same transaction as the alert, so an
/// alert is never lost, and is retried with growing delays until it is delivered.
/// </summary>
public sealed class OutboxMessage
{
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromHours(1);

    private OutboxMessage()
    {
        Recipient = null!;
        Subject = null!;
        Body = null!;
    }

    public OutboxMessage(Alert alert, MessageChannel channel, string recipient, string subject, string body, DateTimeOffset nowUtc)
    {
        Alert = alert;
        Channel = channel;
        Recipient = Guard.NotBlank(recipient, nameof(recipient));
        Subject = subject;
        Body = body;
        CreatedAtUtc = nowUtc;
        NextAttemptAtUtc = nowUtc;
    }

    public long Id { get; private set; }

    public long AlertId { get; private set; }

    public Alert Alert { get; private set; } = null!;

    public MessageChannel Channel { get; private set; }

    /// <summary>An e-mail address or a phone number.</summary>
    public string Recipient { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? SentAtUtc { get; private set; }

    /// <summary>Why the last attempt failed, or a note about how it was delivered.</summary>
    public string? LastError { get; private set; }

    public bool IsSent => SentAtUtc is not null;

    public void Delivered(DateTimeOffset nowUtc, string? note = null)
    {
        Attempts++;
        SentAtUtc = nowUtc;
        LastError = note;
    }

    /// <summary>Schedules another attempt: 30 s, 1 min, 2 min, ... at most an hour apart.</summary>
    public void AttemptFailed(string error, DateTimeOffset nowUtc)
    {
        Attempts++;
        LastError = error.Length > 500 ? error[..500] : error;
        var delay = FirstRetryDelay * Math.Pow(2, Math.Min(Attempts - 1, 10));
        NextAttemptAtUtc = nowUtc + (delay < MaxRetryDelay ? delay : MaxRetryDelay);
    }
}
