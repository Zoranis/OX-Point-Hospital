using System.ComponentModel.DataAnnotations;

namespace OxBox.Infrastructure.Alerts;

/// <summary>The mail server alert e-mails are sent through. Leave <see cref="Host"/> empty to keep e-mails queued.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>Use STARTTLS or implicit TLS when the server offers it. Turn off only for a local test server.</summary>
    public bool UseTls { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string From { get; set; } = "oxbox@localhost";

    public string FromName { get; set; } = "OxBox";
}
