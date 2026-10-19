namespace OxBox.Domain.Alerts;

/// <summary>
/// One alert condition, from when it was raised until it cleared. While an alert is active, the
/// same condition (the same <see cref="Key"/>) is not raised again, so each one is sent once.
/// Clearing re-arms it.
/// </summary>
public sealed class Alert
{
    private Alert()
    {
        Key = null!;
        BoxCode = null!;
        Details = null!;
    }

    public Alert(AlertKind kind, string key, string boxCode, int costCentreId, string details, DateTimeOffset nowUtc)
    {
        Kind = kind;
        Key = Guard.NotBlank(key, nameof(key));
        BoxCode = boxCode;
        CostCentreId = costCentreId;
        Details = details;
        RaisedAtUtc = nowUtc;
    }

    public long Id { get; private set; }

    public AlertKind Kind { get; private set; }

    /// <summary>Identifies the condition, e.g. <c>LowStock:BOX-01:1101</c>.</summary>
    public string Key { get; private set; }

    public string BoxCode { get; private set; }

    public int CostCentreId { get; private set; }

    /// <summary>A short English description for the web app; e-mails are worded separately.</summary>
    public string Details { get; private set; }

    public DateTimeOffset RaisedAtUtc { get; private set; }

    public DateTimeOffset? ClearedAtUtc { get; private set; }

    public bool IsActive => ClearedAtUtc is null;

    public void Resolve(DateTimeOffset nowUtc)
    {
        if (IsActive)
        {
            ClearedAtUtc = nowUtc;
        }
    }
}
