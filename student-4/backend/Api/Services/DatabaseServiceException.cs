namespace Api.Services;

// Surfaced when the internal database service is unreachable or
// returns an unexpected response. Mapped to 503 by the API.
public sealed class DatabaseServiceException : Exception
{
    public DatabaseServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
