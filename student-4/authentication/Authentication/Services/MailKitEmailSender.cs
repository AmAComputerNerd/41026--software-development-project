using Authentication.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Authentication.Services;

// Sends email via SMTP using MailKit. Works against any RFC 5321
// SMTP host - MailHog in dev (plain text, no auth, port 1025),
// real providers in production (TLS, username/password, port 587).
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
            // MailHog / unauthenticated local SMTP: don't validate the
            // server certificate. Real providers will set UseSsl=true
            // and we accept their cert as long as the framework trusts
            // it (default trust store on the image).
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
            // Wrap so the endpoint can return a stable shape - we never
            // want to leak the SMTP host or credentials in a response.
            throw new EmailSendException(
                $"Failed to send email to {toAddress} via {smtp.Host}:{smtp.Port}.",
                ex);
        }
    }
}
