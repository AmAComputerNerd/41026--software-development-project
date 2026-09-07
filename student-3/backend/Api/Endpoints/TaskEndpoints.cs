using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
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
        group.MapPut("/{id:guid}", UpdateTask);
        group.MapDelete("/{id:guid}", DeleteTask);
        return endpoints;
    }

    private static async Task<IResult> GetTasks(
        [AsParameters] TaskFilterDto filter,
        IStudent3DatabaseClient database,
        CancellationToken cancellationToken)
    {
        var tasks = await database.GetTasksAsync(filter, cancellationToken);
        return Results.Ok(tasks.Select(task => task.ToDto()));
    }

    private static async Task<IResult> GetTask(
        [FromRoute] Guid id,
        IStudent3DatabaseClient database,
        CancellationToken cancellationToken)
    {
        var task = await database.GetTaskAsync(id, cancellationToken);
        return task is null ? Results.NotFound() : Results.Ok(task.ToDto());
    }

    private static async Task<IResult> AddTask(
        CreateTaskRequestDto request,
        IStudent3DatabaseClient database,
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

        return Results.Created($"/api/tasks/{task.Id}", task.ToDto());
    }

    private static async Task<IResult> GenerateBreakdown(
        [FromRoute] Guid id,
        GenerateTaskBreakdownRequestDto request,
        IStudent3DatabaseClient database,
        IAiTaskService aiTaskService,
        CancellationToken cancellationToken)
    {
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

        var assignment = await database.GetTaskAsync(id, cancellationToken);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        var generatedTasks = await aiTaskService.GenerateSubtasksAsync(
            new AiTaskContext(
                assignment.Title,
                assignment.Description,
                assignment.CourseName,
                null,
                assignment.DueDate),
            prompt,
            cancellationToken);
        var tasks = await database.CreateSubtasksAsync(
            assignment.Id,
            new CreateSubtasksCommand(
                request.Priority,
                generatedTasks
                    .Select(task => new GeneratedSubtaskRecord(task.Title, task.Description))
                    .ToList()),
            cancellationToken);

        return tasks is null
            ? Results.NotFound()
            : Results.Created(
                $"/api/tasks/{assignment.Id}",
                tasks.Select(task => task.ToDto()).ToList());
    }

    private static async Task<IResult> GenerateDescription(
        GenerateTaskDescriptionRequestDto request,
        IStudent3DatabaseClient database,
        IAiTaskService aiTaskService,
        CancellationToken cancellationToken)
    {
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
        IStudent3DatabaseClient database,
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
        IStudent3DatabaseClient database,
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
