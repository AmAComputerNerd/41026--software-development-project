using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.DTOs;
using Api.Extensions;
using Student3.Contracts;

namespace Api.Services;

public sealed partial class AssignmentBreakdownService(
    IMultiAgentClient client,
    IDatabaseClient database,
    INotificationClient notifications,
    ILogger<AssignmentBreakdownService> logger)
{
    private const string Feature = "student-3-assignment-breakdown";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BreakdownProposalDto?> GenerateAsync(
        Guid assignmentId, GenerateTaskBreakdownRequestDto request, CancellationToken cancellationToken)
    {
        var assignment = await database.GetTaskAsync(assignmentId, cancellationToken);
        if (assignment is null) return null;
        EnsureApplicable(assignment);
        var evidence = AssignmentContext(assignment, true);
        var goal = new WorkflowGoal(
            Feature, assignment.Id, request.Priority, Fingerprint(AssignmentContext(assignment)), request.Prompt.Trim(),
            "Return only compact JSON: {\"tasks\":[{\"title\":\"Action\",\"description\":\"Short description\"}]}. " +
            "Create 2-10 distinct assignment subtasks; titles at most 120 characters, descriptions at most 500. " +
            "Use the supplied assignment context, not notification triage. Keep the response within the worker's output budget.");
        var workflow = await client.RunAsync(
            JsonSerializer.Serialize(goal, JsonOptions), evidence, cancellationToken);
        if (workflow.Status != "AwaitingHumanReview" || workflow.HumanReview is not null)
        {
            throw new MultiAgentServiceException("The Multi-Agent server did not return a proposal awaiting human review.");
        }
        return new BreakdownProposalDto(workflow, ParseTasks(workflow.Worker.DraftOutput, 2));
    }

    public async Task<BreakdownReviewResultDto?> ReviewAsync(
        Guid assignmentId, SubmitBreakdownReviewRequestDto request, CancellationToken cancellationToken)
    {
        var workflow = await client.GetAsync(request.WorkflowId, cancellationToken);
        if (workflow is null) return null;
        var goal = ReadGoal(workflow.Goal);
        if (goal.Feature != Feature || goal.AssignmentId != assignmentId) return null;
        var assignment = await database.GetTaskAsync(assignmentId, cancellationToken);
        if (assignment is null) return null;
        if (!request.Reject)
        {
            EnsureApplicable(assignment);
            if (goal.AssignmentFingerprint != Fingerprint(AssignmentContext(assignment)))
            {
                throw new BreakdownConflictException("The assignment changed. Generate a new proposal before saving tasks.");
            }
        }
        var original = ParseTasks(workflow.Worker.DraftOutput, 2);
        if (request.Tasks is null || request.Tasks.Count > original.Count ||
            request.Tasks.Any(task => task is null || task.Index < 0 || task.Index >= original.Count) ||
            request.Tasks.Select(task => task.Index).Distinct().Count() != request.Tasks.Count ||
            (request.Reject && request.Tasks.Count != 0))
        {
            throw new BreakdownValidationException("Select distinct tasks from this proposal, or reject it without tasks.");
        }
        var selected = request.Tasks.OrderBy(task => task.Index)
            .Select(task => new GeneratedSubtask(task.Title?.Trim() ?? "",
                string.IsNullOrWhiteSpace(task.Description) ? null : task.Description.Trim())).ToList();
        if (!request.Reject && !TaskBreakdownValidation.IsValid(selected, 1))
        {
            throw new BreakdownValidationException("Select 1-10 distinct tasks with titles of 1-120 characters and descriptions of at most 500.");
        }
        var decision = request.Reject ? "Reject" :
            selected.Count < original.Count ? "PartialAccept" :
            selected.SequenceEqual(original) ? "Approve" : "Correct";
        var wasAwaiting = workflow.Status == "AwaitingHumanReview" && workflow.HumanReview is null;
        if (wasAwaiting)
        {
            workflow = await client.ReviewAsync(workflow.Id, new MultiAgentReviewRequestDto(
                decision, request.ReviewerName.Trim(), request.Comments.Trim(),
                decision is "Correct" or "PartialAccept" ? SerializeTasks(selected) : null), cancellationToken);
        }
        var review = workflow.HumanReview;
        var expectedStatus = decision switch
        {
            "Approve" => "Approved",
            "Correct" => "Corrected",
            "PartialAccept" => "PartiallyAccepted",
            _ => "Rejected"
        };
        if (workflow.Id != request.WorkflowId || workflow.Status != expectedStatus ||
            review is null || review.Decision != decision ||
            review.ReviewerName != request.ReviewerName.Trim() || review.Comments != request.Comments.Trim() ||
            (!request.Reject && !ParseTasks(review.FinalOutput, 1).SequenceEqual(selected)))
        {
            throw new BreakdownConflictException("A different human decision is already recorded for this proposal.");
        }
        if (request.Reject) return new BreakdownReviewResultDto(workflow, []);
        var tasks = await database.CreateSubtasksAsync(assignmentId, new CreateSubtasksCommand(
            goal.Priority,
            selected.Select(task => new GeneratedSubtaskRecord(task.Title, task.Description)).ToList(),
            workflow.Id), cancellationToken);
        if (wasAwaiting && tasks is { Count: > 0 })
        {
            try
            {
                await notifications.PushAsync(new PushNotificationDto(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Deadline", "deadlines",
                    $"{tasks.Count} reviewed subtask(s) created for \"{assignment.Title}\".",
                    "Task", assignment.Id), cancellationToken);
            }
            catch (Exception exception)
            {
                LogNotificationFailure(logger, assignment.Id, exception);
            }
        }
        return tasks is null ? null : new BreakdownReviewResultDto(
            workflow, tasks.Select(task => task.ToDto()).ToList());
    }

    private static WorkflowGoal ReadGoal(string goal)
    {
        try
        {
            return JsonSerializer.Deserialize<WorkflowGoal>(goal, JsonOptions)
                ?? throw new JsonException("Missing assignment workflow context.");
        }
        catch (JsonException)
        {
            throw new BreakdownConflictException("This workflow is not an assignment breakdown proposal.");
        }
    }

    private static List<GeneratedSubtask> ParseTasks(string output, int minimum)
    {
        try
        {
            var text = output.Trim();
            if (text.StartsWith("```", StringComparison.Ordinal) && text.EndsWith("```", StringComparison.Ordinal))
            {
                var firstLine = text.IndexOf('\n');
                if (firstLine >= 0) text = text[(firstLine + 1)..^3].Trim();
            }
            var payload = JsonSerializer.Deserialize<TaskPayload>(text, JsonOptions);
            var tasks = payload?.Tasks?.Select(task => new GeneratedSubtask(
                task?.Title?.Trim() ?? "",
                string.IsNullOrWhiteSpace(task?.Description) ? null : task.Description.Trim())).ToList();
            if (tasks is null || !TaskBreakdownValidation.IsValid(tasks, minimum))
            {
                throw new JsonException("Invalid subtask output.");
            }
            return tasks;
        }
        catch (JsonException exception)
        {
            throw new MultiAgentServiceException(
                "The Multi-Agent server did not return a valid structured task breakdown. No tasks were created. Try generating again.",
                exception);
        }
    }

    private static string AssignmentContext(TaskRecord assignment, bool bounded = false) =>
        JsonSerializer.Serialize(new
        {
            assignment.Id,
            Title = bounded ? Limit(assignment.Title, 500) : assignment.Title,
            Description = bounded ? Limit(assignment.Description, 12000) : assignment.Description,
            assignment.CourseId,
            CourseName = bounded ? Limit(assignment.CourseName, 300) : assignment.CourseName,
            assignment.DueDate
        }, JsonOptions);

    private static string? Limit(string? value, int maximum) =>
        value?.Length > maximum ? value[..maximum] : value;

    private static string Fingerprint(string evidence) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));

    private static string SerializeTasks(IReadOnlyList<GeneratedSubtask> tasks) =>
        JsonSerializer.Serialize(new TaskPayload(tasks), JsonOptions);

    private static void EnsureApplicable(TaskRecord assignment)
    {
        if (assignment.CanvasIsActive is false || assignment.Status == "Completed")
        {
            throw new BreakdownConflictException("This assignment is inactive or completed.");
        }
    }

    private sealed record WorkflowGoal(
        string Feature, Guid AssignmentId, string Priority, string AssignmentFingerprint,
        string Prompt, string OutputFormat);

    private sealed record TaskPayload(IReadOnlyList<GeneratedSubtask>? Tasks);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not send the reviewed breakdown notification for assignment {AssignmentId}.")]
    private static partial void LogNotificationFailure(ILogger logger, Guid assignmentId, Exception exception);
}

public sealed class BreakdownValidationException(string message) : Exception(message);
public sealed class BreakdownConflictException(string message) : Exception(message);
