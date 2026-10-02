using Authentication.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Authentication.Services;

// Sends email via SMTP using MailKit.
public sealed partial class MailKitEmailSender(
    IOptions<EmailOptions> options,
    ILogger<MailKitEmailSender> logger) : IEmailSender
{
    [LoggerMessage(LogLevel.Information, "Sent email to {To} with subject '{Subject}' via {Host}:{Port}.")]
    private static partial void LogEmailSent(
        ILogger logger,
        string to,
        string subject,
        string host,
        int port);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Email configuration loaded: Host={Host}, Port={Port}, UseSsl={UseSsl}, Username={Username}, FromAddress={FromAddress}, FromName={FromName}")]
    public static partial void LogEmailConfiguration(ILogger logger, string host, int port, bool useSsl, string username, string fromAddress, string fromName);

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

            // Determine SSL mode: port 465 = implicit TLS (SslOnConnect), 587 = explicit TLS (StartTls)
            var sslMode = smtp.UseSsl
                ? (smtp.Port == 465
                    ? MailKit.Security.SecureSocketOptions.SslOnConnect
                    : MailKit.Security.SecureSocketOptions.StartTls)
                : MailKit.Security.SecureSocketOptions.None;

            await client.ConnectAsync(
                smtp.Host,
                smtp.Port,
                sslMode,
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

            LogEmailSent(_logger, toAddress, subject, smtp.Host, smtp.Port);
        }
        catch (Exception ex)
        {
            throw new EmailSendException(
                $"Failed to send email to {toAddress} via {smtp.Host}:{smtp.Port}.",
                ex);
        }
    }
}
