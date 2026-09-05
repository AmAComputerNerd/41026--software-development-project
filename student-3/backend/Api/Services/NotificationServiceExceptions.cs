namespace Api.Services;

public sealed class NotificationServiceException : Exception
{
    public NotificationServiceException(string message) : base(message)
    {
    }

    public NotificationServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
