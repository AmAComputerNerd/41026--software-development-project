using Api.Models;

namespace Api.Services.Executors;

public sealed class AutomationExecutorRegistry(IEnumerable<IAutomationExecutor> executors)
{
    private readonly Dictionary<Type, IAutomationExecutor> _executors = executors
        .ToDictionary(executor => executor.AutomationType);

    public IAutomationExecutor GetExecutor(Automation automation)
    {
        return _executors.TryGetValue(automation.GetType(), out var executor)
            ? executor
            : throw new InvalidOperationException(
                $"No executor is registered for {automation.GetType().Name}.");
    }
}