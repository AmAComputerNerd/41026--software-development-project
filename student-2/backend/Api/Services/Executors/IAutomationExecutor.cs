using Api.Models;

namespace Api.Services.Executors;

public interface IAutomationExecutionCandidate
{
    string ExecutionKey { get; }
    bool MatchesRun(AutomationRun run);
    AutomationRun CreateRun(DateTime startedAtUtc);
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public interface IAutomationExecutor
{
    Type AutomationType { get; }
    Task<IReadOnlyList<IAutomationExecutionCandidate>> GetDueExecutionsAsync(
        Automation automation,
        DateTime utcNow,
        CancellationToken cancellationToken);
}

public abstract class AutomationExecutor<TAutomation> : IAutomationExecutor
    where TAutomation : Automation
{
    public Type AutomationType => typeof(TAutomation);

    public Task<IReadOnlyList<IAutomationExecutionCandidate>> GetDueExecutionsAsync(
        Automation automation,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return GetDueExecutionsAsync((TAutomation)automation, utcNow, cancellationToken);
    }

    protected abstract Task<IReadOnlyList<IAutomationExecutionCandidate>> GetDueExecutionsAsync(
        TAutomation automation,
        DateTime utcNow,
        CancellationToken cancellationToken);
}