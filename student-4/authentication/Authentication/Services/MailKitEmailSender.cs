using Authentication.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Authentication.Services;

// Sends email via SMTP using MailKit.
public sealed class MailKitEmailSender(
    IOptions<EmailOptions> options,
    ILogger<MailKitEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private readonly ILogger<MailKitEmailSender> _logger = logger;

    public async Task SendAsync(
        string toAddress,
        string toName,
        string subject,
        string textBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        message.Subject = subject;

        var body = new BodyBuilder
        {
            TextBody = textBody,
        };
        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            body.HtmlBody = htmlBody;
        }
        message.Body = body.ToMessageBody();

        var smtp = _options.Smtp;
        using var client = new SmtpClient();

        try
        {
            if (!smtp.UseSsl)
            {
                client.CheckCertificateRevocation = false;
            }

            await client.ConnectAsync(
                smtp.Host,
                smtp.Port,
                smtp.UseSsl
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                await client.AuthenticateAsync(
                    smtp.Username,
                    smtp.Password ?? string.Empty,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation(
                "Sent email to {To} with subject '{Subject}' via {Host}:{Port}.",
                toAddress, subject, smtp.Host, smtp.Port);
        }
        catch (Exception ex)
        {
            throw new EmailSendException(
                $"Failed to send email to {toAddress} via {smtp.Host}:{smtp.Port}.",
                ex);
        }
    }
}
