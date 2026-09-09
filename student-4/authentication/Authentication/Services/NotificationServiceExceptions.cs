namespace Authentication.Services;

public sealed class NotificationServiceException : Exception
{
    public NotificationServiceException()
    {
    }

    public NotificationServiceException(string message) : base(message)
    {
    }

    public NotificationServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
