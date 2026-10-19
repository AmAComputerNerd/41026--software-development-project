using Api.Configuration;
using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Student3.Contracts;

namespace Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tasks");
        group.MapGet("/", GetTasks);
        group.MapGet("/{id:guid}", GetTask);
        group.MapPost("/", AddTask);
        group.MapPost("/ai-description", GenerateDescription);
        group.MapPost("/{id:guid}/ai-breakdown", GenerateBreakdown);
        group.MapPost("/{id:guid}/ai-breakdown/review", ReviewBreakdown);
        group.MapPut("/{id:guid}", UpdateTask);
        group.MapDelete("/{id:guid}", DeleteTask);
        return endpoints;
    }

    private static async Task<IResult> GetTasks(
        [AsParameters] TaskFilterDto filter,
        IDatabaseClient database,
        CancellationToken cancellationToken)
    {
        var tasks = await database.GetTasksAsync(filter, cancellationToken);
        return Results.Ok(tasks.Select(task => task.ToDto()));
    }

    private static async Task<IResult> GetTask(
        [FromRoute] Guid id,
        IDatabaseClient database,
        CancellationToken cancellationToken)
    {
        var task = await database.GetTaskAsync(id, cancellationToken);
        return task is null ? Results.NotFound() : Results.Ok(task.ToDto());
    }

    private static readonly Guid DemoStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static string FormatDate(DateTime value)
    {
        return value.ToString("MMM d, yyyy 'at' h:mm tt 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<IResult> AddTask(
        CreateTaskRequestDto request,
        IDatabaseClient database,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest("`title` is a required argument");
        }

        if (!IsTaskPriority(request.Priority))
        {
            return Results.BadRequest(
                "`priority` could not be correlated with a valid TaskPriority");
        }

        if (request.CourseId.HasValue &&
            await database.GetCourseAsync(request.CourseId.Value, cancellationToken) is null)
        {
            return Results.BadRequest("Specified course does not exist.");
        }

        if (request.ParentTaskId.HasValue &&
            await database.GetTaskAsync(request.ParentTaskId.Value, cancellationToken) is null)
        {
            return Results.BadRequest("Specified parentTask does not exist.");
        }

        var task = await database.CreateTaskAsync(
            new CreateTaskCommand(
                request.Title,
                request.Description,
                request.DueDate,
                request.Priority,
                request.CourseId,
                request.ParentTaskId),
            cancellationToken);

        try
        {
            var message = task.DueDate.HasValue
                ? $"New task \"{task.Title}\" is due {FormatDate(task.DueDate.Value)}."
                : $"New task \"{task.Title}\" created.";

            await notificationClient.PushAsync(
                new PushNotificationDto(
                    StudentId: DemoStudentId,
                    Type: "Deadline",
                    SourceMicroservice: "deadlines",
                    Message: message,
                    RelatedEntityType: "Task",
                    RelatedEntityId: task.Id),
                cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger("Api.Endpoints.TaskEndpoints");
            TaskEndpointsLog.PushNotificationFailed(logger, task.Id, ex);
        }

        return Results.Created($"/api/tasks/{task.Id}", task.ToDto());
    }

    private static async Task<IResult> GenerateBreakdown(
        [FromRoute] Guid id,
        GenerateTaskBreakdownRequestDto request,
        IOptions<AiGatewayOptions> options,
        AssignmentBreakdownService breakdown,
        IMultiAgentClient multiAgent,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is false)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI Mode is disabled.");
        }
        if (!multiAgent.IsEnabled) throw new MultiAgentIntegrationDisabledException();

        var prompt = request.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 2000)
        {
            return Results.BadRequest("`prompt` must contain between 1 and 2000 characters.");
        }

        if (!IsTaskPriority(request.Priority))
        {
            return Results.BadRequest(
                "`priority` could not be correlated with a valid TaskPriority.");
        }

        var proposal = await breakdown.GenerateAsync(id, request with { Prompt = prompt }, cancellationToken);
        return proposal is null ? Results.NotFound() : Results.Ok(proposal);
    }

    private static async Task<IResult> ReviewBreakdown(
        Guid id,
        SubmitBreakdownReviewRequestDto request,
        IOptions<AiGatewayOptions> options,
        IMultiAgentClient multiAgent,
        AssignmentBreakdownService breakdown,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is not true) throw new AiGatewayConfigurationException("AI Mode is disabled.");
        if (!multiAgent.IsEnabled) throw new MultiAgentIntegrationDisabledException();
        if (request.WorkflowId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.ReviewerName) || request.ReviewerName.Length > 100 ||
            string.IsNullOrWhiteSpace(request.Comments) || request.Comments.Length > 2000)
        {
            return Results.BadRequest("Supply a workflow ID, reviewer name (1-100 characters) and review comments (1-2000 characters).");
        }
        var result = await breakdown.ReviewAsync(id, request, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GenerateDescription(
        GenerateTaskDescriptionRequestDto request,
        IOptions<AiGatewayOptions> options,
        IDatabaseClient database,
        IAiTaskService aiTaskService,
        CancellationToken cancellationToken)
    {
        if (options.Value.Enabled is false)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI Mode is disabled.");
        }

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            return Results.BadRequest("`title` must contain between 1 and 200 characters.");
        }

        CourseRecord? course = null;
        if (request.CourseId.HasValue)
        {
            course = await database.GetCourseAsync(request.CourseId.Value, cancellationToken);
            if (course is null)
            {
                return Results.BadRequest("Specified course does not exist.");
            }
        }

        TaskRecord? parentTask = null;
        if (request.ParentTaskId.HasValue)
        {
            parentTask = await database.GetTaskAsync(
                request.ParentTaskId.Value,
                cancellationToken);
            if (parentTask is null)
            {
                return Results.BadRequest("Specified parentTask does not exist.");
            }

            if (course is not null && parentTask.CourseId != course.Id)
            {
                return Results.BadRequest(
                    "The specified course does not match the parent task.");
            }

            if (course is null && parentTask.CourseId.HasValue)
            {
                course = await database.GetCourseAsync(
                    parentTask.CourseId.Value,
                    cancellationToken);
            }
        }

        var description = await aiTaskService.GenerateDescriptionAsync(
            new AiTaskContext(
                title,
                null,
                course?.Name,
                parentTask?.Title,
                parentTask?.DueDate),
            cancellationToken);

        return Results.Ok(new GeneratedTaskDescriptionDto(description));
    }

    private static async Task<IResult> UpdateTask(
        [FromRoute] Guid id,
        ModifyTaskRequestDto request,
        IDatabaseClient database,
        CancellationToken cancellationToken)
    {
        var existing = await database.GetTaskAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        if (existing.CanvasAssignmentId.HasValue &&
            (request.NewTitle is not null ||
             request.UpdateDescription ||
             request.UpdateDueDate))
        {
            return Results.BadRequest(
                "Canvas-synced task titles, descriptions, and due dates cannot be updated.");
        }

        var task = await database.UpdateTaskAsync(
            id,
            new UpdateTaskCommand(
                request.NewTitle,
                request.UpdateDescription,
                request.NewDescription,
                request.UpdateDueDate,
                request.NewDueDate,
                request.NewPriority,
                request.NewStatus),
            cancellationToken);

        return task is null ? Results.NotFound() : Results.Ok(task.ToDto());
    }

    private static async Task<IResult> DeleteTask(
        [FromRoute] Guid id,
        IDatabaseClient database,
        CancellationToken cancellationToken)
    {
        return await database.DeleteTaskAsync(id, cancellationToken)
            ? Results.Ok()
            : Results.NotFound();
    }

    private static bool IsTaskPriority(string? value)
    {
        return value is "Low" or "Medium" or "High";
    }
}

internal static partial class TaskEndpointsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for task {TaskId}.")]
    public static partial void PushNotificationFailed(ILogger logger, Guid taskId, Exception ex);
}
