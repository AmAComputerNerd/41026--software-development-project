namespace Api.Services;

public sealed class AiGatewayException : Exception
{
    public int? UpstreamStatusCode { get; }
    public string? UpstreamErrorBody { get; }
    public DateTimeOffset? RateLimitReset { get; }

    public AiGatewayException(
        string message,
        int? upstreamStatusCode = null,
        string? upstreamErrorBody = null,
        DateTimeOffset? rateLimitReset = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        UpstreamStatusCode = upstreamStatusCode;
        UpstreamErrorBody = upstreamErrorBody;
        RateLimitReset = rateLimitReset;
    }
}
