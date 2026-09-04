using System.Security.Cryptography;
using System.Text.Json;
using Api.DTOs;
using Api.Extensions;
using Api.Models;

namespace Api.Services.Executors;

public sealed class ScheduledPostAutomationExecutor(ISharedCanvasClient canvas)
    : AutomationExecutor<ScheduledPostAutomation>
{
    protected override Task<IReadOnlyList<IAutomationExecutionCandidate>> GetDueExecutionsAsync(
        ScheduledPostAutomation automation,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (automation.PostTime > utcNow)
        {
            return Task.FromResult<IReadOnlyList<IAutomationExecutionCandidate>>([]);
        }

        var snapshot = new ScheduledPostExecutionSnapshot(
            automation.Id,
            automation.PostTime,
            automation.ContextCode,
            RecipientJson.Deserialize(automation.Recipients)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            automation.Subject,
            automation.Body,
            automation.GroupConversation);

        return Task.FromResult<IReadOnlyList<IAutomationExecutionCandidate>>(
            [new ScheduledPostExecutionCandidate(canvas, snapshot)]);
    }

    private sealed class ScheduledPostExecutionCandidate(
        ISharedCanvasClient canvas,
        ScheduledPostExecutionSnapshot snapshot) : IAutomationExecutionCandidate
    {
        public string ExecutionKey { get; } = CreateExecutionKey(snapshot);

        public bool MatchesRun(AutomationRun run)
        {
            if (run is not ScheduledPostAutomationRun scheduledRun)
            {
                return false;
            }

            return scheduledRun.PostTime == snapshot.PostTime &&
                scheduledRun.ContextCode == snapshot.ContextCode &&
                scheduledRun.Subject == snapshot.Subject &&
                scheduledRun.Body == snapshot.Body &&
                scheduledRun.GroupConversation == snapshot.GroupConversation &&
                RecipientJson.Deserialize(scheduledRun.Recipients)
                    .Order(StringComparer.Ordinal)
                    .SequenceEqual(snapshot.Recipients, StringComparer.Ordinal);
        }

        public AutomationRun CreateRun(DateTime startedAtUtc)
        {
            return new ScheduledPostAutomationRun
            {
                AutomationId = snapshot.AutomationId,
                ExecutionKey = ExecutionKey,
                ExecutionTimeStamp = startedAtUtc,
                Result = AutomationRunResult.Running,
                PostTime = snapshot.PostTime,
                ContextCode = snapshot.ContextCode,
                Recipients = RecipientJson.Serialize(snapshot.Recipients),
                Subject = snapshot.Subject,
                Body = snapshot.Body,
                GroupConversation = snapshot.GroupConversation
            };
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await canvas.CreateConversationAsync(
                new CreateSharedCanvasConversationDto(
                    snapshot.Recipients,
                    snapshot.Subject,
                    snapshot.Body,
                    snapshot.ContextCode,
                    snapshot.GroupConversation),
                cancellationToken);
        }

        private static string CreateExecutionKey(ScheduledPostExecutionSnapshot execution)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(execution);
            return $"scheduled-post:v1:{Convert.ToHexString(SHA256.HashData(payload))}";
        }
    }

    private sealed record ScheduledPostExecutionSnapshot(
        Guid AutomationId,
        DateTime PostTime,
        string ContextCode,
        string[] Recipients,
        string Subject,
        string Body,
        bool GroupConversation);
}