using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OxBox.Domain.Alerts;

namespace OxBox.Infrastructure.Alerts;

internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IMessageSender
{
    public MessageChannel Channel => MessageChannel.Email;

    public async Task<string?> SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException("No mail server is configured (Smtp:Host).");
        }

        var mail = new MimeMessage();
        mail.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
        mail.To.Add(MailboxAddress.Parse(message.Recipient));
        mail.Subject = message.Subject;
        mail.Body = new TextPart("plain") { Text = message.Body };

        using var client = new SmtpClient();
        var security = smtp.UseTls ? SecureSocketOptions.Auto : SecureSocketOptions.None;
        await client.ConnectAsync(smtp.Host, smtp.Port, security, cancellationToken);
        if (!string.IsNullOrEmpty(smtp.UserName))
        {
            await client.AuthenticateAsync(smtp.UserName, smtp.Password, cancellationToken);
        }

        await client.SendAsync(mail, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
        return null;
    }
}
