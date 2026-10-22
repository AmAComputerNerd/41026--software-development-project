using GradesManager.Contracts;
using GradesManager.DTOs;

namespace GradesManager.Services;

public interface IMultiAgentClient
{
    bool IsEnabled { get; }

    Task<MultiAgentWorkflowDto> RunAsync(
        IReadOnlyList<AssignmentRecord> assignments, CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto> ReviewAsync(
        Guid id, MultiAgentReviewRequestDto request, CancellationToken cancellationToken);

    Task<MultiAgentWorkflowDto> ReviseAsync(
        Guid id, MultiAgentRevisionRequestDto request, CancellationToken cancellationToken);
}