using System.Net;
using Api.DTOs;

namespace Api.Services;

public interface IMultiAgentClient
{
    bool IsEnabled { get; }

    Task<MultiAgentWorkflowDto> RunAsync(
        Guid studentId,
        string goal,
        string evidenceSnapshot,
        CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto> ReviewAsync(
        Guid id,
        MultiAgentReviewRequestDto request,
        CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto> ReviseAsync(
        Guid id,
        MultiAgentRevisionRequestDto request,
        CancellationToken cancellationToken);
}

public sealed class MultiAgentIntegrationDisabledException()
    : Exception("Multi-Agent integration is disabled.");

public sealed class MultiAgentConflictException(string message) : Exception(message);

public sealed class MultiAgentServiceException(
    string message,
    Exception? innerException = null,
    HttpStatusCode? statusCode = null) : Exception(message, innerException)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
