using Api.Configuration;
using Api.Data;
using Api.Models;
using Api.Services.Executors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Services;

public sealed class AutomationExecutionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<AutomationExecutionOptions> options,
    ILogger<AutomationExecutionBackgroundService> logger) : BackgroundService
{
    private const int SqliteUniqueConstraintErrorCode = 2067;
    private static readonly Action<ILogger, Exception?> LogCycleFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, nameof(LogCycleFailure)),
        "Automation execution cycle failed.");
    private static readonly Action<ILogger, Guid, string, Exception?> LogClaimCollision =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Debug,
            new EventId(2, nameof(LogClaimCollision)),
            "Automation {AutomationId} execution {ExecutionKey} was claimed elsewhere.");
    private static readonly Action<ILogger, Guid, Exception?> LogExecutionFailure =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(3, nameof(LogExecutionFailure)),
            "Automation {AutomationId} execution failed.");
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCycleSafelyAsync(stoppingToken);
        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCycleSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunCycleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunCycleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogCycleFailure(logger, exception);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var automationIds = await db.Automations
            .AsNoTracking()
            .Where(automation => automation.Enabled && !automation.Deleted)
            .Select(automation => automation.Id)
            .ToListAsync(cancellationToken);

        foreach (var automationId in automationIds)
        {
            await ExecuteAutomationAsync(automationId, cancellationToken);
        }
    }

    private async Task ExecuteAutomationAsync(
        Guid automationId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<AutomationExecutorRegistry>();
        var automation = await db.Automations.FindAsync([automationId], cancellationToken);
        if (automation is null || !automation.Enabled || automation.Deleted)
        {
            return;
        }

        var executor = registry.GetExecutor(automation);
        var now = DateTime.UtcNow;
        var candidates = await executor.GetDueExecutionsAsync(
            automation,
            now,
            cancellationToken);
        var existingRuns = await db.AutomationRuns
            .Where(run => run.AutomationId == automation.Id)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (existingRuns.Any(run =>
                    run.ExecutionKey == candidate.ExecutionKey || candidate.MatchesRun(run)))
            {
                continue;
            }

            var run = candidate.CreateRun(now);
            db.AutomationRuns.Add(run);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqliteException
                {
                    SqliteExtendedErrorCode: SqliteUniqueConstraintErrorCode
                })
            {
                db.Entry(run).State = EntityState.Detached;
                LogClaimCollision(logger, automation.Id, candidate.ExecutionKey, null);
                continue;
            }

            existingRuns.Add(run);
            try
            {
                await candidate.ExecuteAsync(cancellationToken);
                run.Result = AutomationRunResult.Success;
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                run.Result = AutomationRunResult.Failed;
                await db.SaveChangesAsync(CancellationToken.None);
                LogExecutionFailure(logger, automation.Id, exception);
            }
        }
    }
}