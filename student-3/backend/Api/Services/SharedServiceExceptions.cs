namespace Api.Services;

public sealed class SharedServiceConfigurationException(string message) : Exception(message);

public sealed class SharedServiceException : Exception
{
    public SharedServiceException(string message) : base(message)
    {
    }

    public SharedServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
