namespace Authentication.Services;

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
