namespace Authentication.Services;

// Sends plain-text and HTML email. Implementations are expected to
// fail soft (throw EmailSendException); the caller is responsible
// for surfacing the failure to the user.
public interface IEmailSender
{
    Task SendAsync(
        string toAddress,
        string toName,
        string subject,
        string textBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default);
}

public sealed class EmailSendException : Exception
{
    public EmailSendException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
