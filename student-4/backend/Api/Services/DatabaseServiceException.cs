namespace Api.Services;

public sealed class DatabaseServiceException : Exception
{
    public DatabaseServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
