namespace OxBox.Domain.Alerts;

/// <summary>
/// Someone who receives alerts by e-mail, SMS or both. Recipients without a cost centre get every
/// alert; recipients of a cost centre also get that cost centre's stock alerts.
/// </summary>
public sealed class AlertRecipient
{
    private AlertRecipient()
    {
        Name = null!;
    }

    public AlertRecipient(string name, string? email, string? phone, int? costCentreId)
    {
        Name = Guard.NotBlank(name, nameof(name));
        ChangeContacts(email, phone);
        CostCentreId = costCentreId;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    /// <summary><c>null</c>: all alerts. Otherwise this cost centre's stock alerts only.</summary>
    public int? CostCentreId { get; private set; }

    public bool IsActive { get; private set; }

    public void ChangeContacts(string? email, string? phone)
    {
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        if (email is null && phone is null)
        {
            throw new ArgumentException("A recipient needs an e-mail address or a phone number.", nameof(email));
        }

        Email = email;
        Phone = phone;
    }

    public void Rename(string name) => Name = Guard.NotBlank(name, nameof(name));

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
