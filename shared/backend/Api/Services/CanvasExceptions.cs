using System.Net;

namespace Api.Services;

public sealed class CanvasConfigurationException(string message) : Exception(message);

public sealed class CanvasApiException(
    HttpStatusCode statusCode,
    string message,
    TimeSpan? retryAfter = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
