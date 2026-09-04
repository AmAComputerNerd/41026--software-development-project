namespace Api.Services;

public sealed class AiGatewayException : Exception
{
    public AiGatewayException(string message) : base(message)
    {
    }

    public AiGatewayException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
