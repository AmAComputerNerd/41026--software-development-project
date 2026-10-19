using Api.DTOs;

namespace Api.Services;

public interface IMultiAgentClient
{
    bool IsEnabled { get; }

    Task<MultiAgentWorkflowDto> RunWorkflowAsync(
        Guid studentId,
        string goal,
        string? evidenceSnapshot = null,
        CancellationToken cancellationToken = default);

    Task<MultiAgentWorkflowDto> SubmitHumanReviewAsync(
        Guid workflowId,
        SubmitHumanReviewRequestDto reviewRequest,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MultiAgentWorkflowDto>> ListWorkflowsAsync(
        Guid? studentId = null,
        CancellationToken cancellationToken = default);

    Task<MultiAgentWorkflowDto?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);
}
