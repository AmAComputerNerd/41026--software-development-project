using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Api.Models.TaskStatus;

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

    private static async Task<IResult> GetTasks([AsParameters] TaskFilterDto filter, AppDbContext db)
    {
        var query = db.Tasks
            .AsNoTracking()
            .Include(t => t.Course)
            .Include(t => t.ParentTask)
            .AsQueryable();

        if (filter.IncludeInactiveCanvas != true)
        {
            query = query.Where(t => t.CanvasIsActive != false);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(t => t.Status.ToString() == filter.Status);
        }

        if (!string.IsNullOrEmpty(filter.Priority))
        {
            query = query.Where(t => t.Priority.ToString() == filter.Priority);
        }

        if (filter.CourseId.HasValue)
        {
            query = query.Where(t => t.CourseId == filter.CourseId.Value);
        }

        if (filter.ParentTaskId.HasValue)
        {
            query = query.Where(t => t.ParentTaskId == filter.ParentTaskId.Value);
        }

        if (filter.Overdue.HasValue)
        {
            var now = DateTime.UtcNow;
            if (filter.Overdue == true)
            {
                query = query.Where(t => t.DueDate < now && t.Status != TaskStatus.Completed);
            }
            else
            {
                query = query.Where(t => t.DueDate > now && t.Status != TaskStatus.Completed);
            }
        }

        var taskDtos = await query.Select(t => t.ToDto()).ToListAsync();

        return Results.Ok(taskDtos);
    }

    private static async Task<IResult> GetTask([FromRoute] Guid id, AppDbContext db)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(t => t.Course)
            .FirstOrDefaultAsync(t => t.Id == id);

        return task == null ? Results.NotFound() : Results.Ok(task.ToDto());
    }

    private static async Task<IResult> AddTask(CreateTaskRequestDto requestDto, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Title))
        {
            return Results.BadRequest("`title` is a required argument");
        }

        if (!Enum.TryParse(requestDto.Priority, out TaskPriority resolvedTaskPriority))
        {
            return Results.BadRequest("`priority` could not be correlated with a valid TaskPriority");
        }

        if (requestDto.CourseId.HasValue)
        {
            var courseExists = await db.Courses.AnyAsync(c => c.Id == requestDto.CourseId.Value);
            if (!courseExists)
            {
                return Results.BadRequest("Specified course does not exist.");
            }
        }

        if (requestDto.ParentTaskId.HasValue)
        {
            var parentTaskExists = await db.Tasks.AnyAsync(c => c.Id == requestDto.ParentTaskId.Value);
            if (!parentTaskExists)
            {
                return Results.BadRequest("Specified parentTask does not exist.");
            }
        }

        var task = new TaskEntity
        {
            Title = requestDto.Title,
            Description = requestDto.Description,
            DueDate = requestDto.DueDate,
            Priority = resolvedTaskPriority,
            Status = TaskStatus.Todo,
            CourseId = requestDto.CourseId,
            ParentTaskId = requestDto.ParentTaskId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        await db.Entry(task)
            .Reference(t => t.Course)
            .LoadAsync();

        return Results.Created(
            $"/api/tasks/{task.Id}",
            task.ToDto()
        );
    }

    private static async Task<IResult> GenerateBreakdown(
        [FromRoute] Guid id,
        GenerateTaskBreakdownRequestDto requestDto,
        AppDbContext db,
        IAiTaskService aiTaskService,
        CancellationToken cancellationToken)
    {
        var prompt = requestDto.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 2000)
        {
            return Results.BadRequest("`prompt` must contain between 1 and 2000 characters.");
        }

        if (!Enum.TryParse(requestDto.Priority, out TaskPriority priority))
        {
            return Results.BadRequest("`priority` could not be correlated with a valid TaskPriority.");
        }

        var assignment = await db.Tasks
            .Include(task => task.Course)
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (assignment is null)
        {
            return Results.NotFound();
        }

        var generatedTasks = await aiTaskService.GenerateSubtasksAsync(
            new AiTaskContext(
                assignment.Title,
                assignment.Description,
                assignment.Course?.Name,
                null,
                assignment.DueDate),
            prompt,
            cancellationToken);
        var now = DateTime.UtcNow;
        var tasks = generatedTasks
            .Select(generated => new TaskEntity
            {
                Title = generated.Title,
                Description = generated.Description,
                DueDate = assignment.DueDate,
                Priority = priority,
                Status = TaskStatus.Todo,
                CourseId = assignment.CourseId,
                Course = assignment.Course,
                ParentTaskId = assignment.Id,
                ParentTask = assignment,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/tasks/{assignment.Id}",
            tasks.Select(task => task.ToDto()).ToList());
    }

    private static async Task<IResult> GenerateDescription(
        GenerateTaskDescriptionRequestDto requestDto,
        AppDbContext db,
        IAiTaskService aiTaskService,
        CancellationToken cancellationToken)
    {
        var title = requestDto.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
        {
            return Results.BadRequest("`title` must contain between 1 and 200 characters.");
        }

        Course? course = null;
        if (requestDto.CourseId.HasValue)
        {
            course = await db.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == requestDto.CourseId.Value,
                    cancellationToken);
            if (course is null)
            {
                return Results.BadRequest("Specified course does not exist.");
            }
        }

        TaskEntity? parentTask = null;
        if (requestDto.ParentTaskId.HasValue)
        {
            parentTask = await db.Tasks
                .AsNoTracking()
                .Include(task => task.Course)
                .FirstOrDefaultAsync(
                    item => item.Id == requestDto.ParentTaskId.Value,
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

            course ??= parentTask.Course;
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
        ModifyTaskRequestDto requestDto,
        AppDbContext db,
        TaskHierarchyService taskHierarchy)
    {
        var task = await db.Tasks
            .FindAsync(id);

        if (task is null)
        {
            return Results.NotFound();
        }

        if (task.CanvasAssignmentId.HasValue &&
            (requestDto.NewTitle is not null ||
             requestDto.UpdateDescription ||
             requestDto.UpdateDueDate))
        {
            return Results.BadRequest(
                "Canvas-synced task titles, descriptions, and due dates cannot be updated.");
        }

        var hasNewPriority = Enum.TryParse(requestDto.NewPriority, out TaskPriority newPriority);
        var hasNewStatus = Enum.TryParse(requestDto.NewStatus, out TaskStatus newStatus);

        task.Title = requestDto.NewTitle ?? task.Title;
        task.Description = requestDto.UpdateDescription ? requestDto.NewDescription : task.Description;
        task.DueDate = requestDto.UpdateDueDate ? requestDto.NewDueDate : task.DueDate;
        task.Priority = hasNewPriority ? newPriority : task.Priority;
        task.Status = hasNewStatus ? newStatus : task.Status;
        var updatedAt = DateTime.UtcNow;
        task.UpdatedAt = updatedAt;

        if (hasNewStatus && newStatus == TaskStatus.Completed)
        {
            await taskHierarchy.CompleteDescendantsAsync(id, updatedAt);
        }

        await db.SaveChangesAsync();

        return Results.Ok(task.ToDto());
    }

    private static async Task<IResult> DeleteTask([FromRoute] Guid id, AppDbContext db)
    {
        var task = await db.Tasks
            .FindAsync(id);

        if (task is null)
        {
            return Results.NotFound();
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();

        return Results.Ok();
    }
}