using Api.Models;

namespace Api.Services.Executors;

public sealed class AssignmentExtensionAutomationExecutor
    : AutomationExecutor<AssignmentExtensionAutomation>
{
    protected override Task<IReadOnlyList<IAutomationExecutionCandidate>> GetDueExecutionsAsync(
        AssignmentExtensionAutomation automation,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<IAutomationExecutionCandidate>>([]);
    }
}