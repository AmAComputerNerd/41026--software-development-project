using Database.Data;
using Database.Extensions;
using Database.Models;
using Database.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student3.Contracts;
using TaskStatus = Database.Models.TaskStatus;

namespace Database.Endpoints;

public static class PersistenceEndpoints
{
    public static IEndpointRouteBuilder MapPersistenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var courses = endpoints.MapGroup("/internal/courses");
        courses.MapGet("/", GetCourses);
        courses.MapGet("/{id:guid}", GetCourse);

        var tasks = endpoints.MapGroup("/internal/tasks");
        tasks.MapGet("/", GetTasks);
        tasks.MapGet("/{id:guid}", GetTask);
        tasks.MapPost("/", CreateTask);
        tasks.MapPut("/{id:guid}", UpdateTask);
        tasks.MapDelete("/{id:guid}", DeleteTask);
        tasks.MapPost("/{id:guid}/subtasks", CreateSubtasks);

        endpoints.MapPost("/internal/canvas-snapshots", ApplyCanvasSnapshot);
        endpoints.MapGet("/internal/reminders/due", GetDueReminders);
        endpoints.MapPut("/internal/reminders/{id:guid}/sent", MarkReminderSent);

        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        AppDbContext db,
        bool includeInactiveCanvas = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Courses.AsNoTracking();
        if (!includeInactiveCanvas)
        {
            query = query.Where(course => course.CanvasIsActive != false);
        }

        var courses = await query.ToListAsync(cancellationToken);
        return Results.Ok(courses.Select(course => course.ToRecord()));
    }

    private static async Task<IResult> GetCourse(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return course is null ? Results.NotFound() : Results.Ok(course.ToRecord());
    }

    private static async Task<IResult> GetTasks(
        AppDbContext db,
        string? status = null,
        string? priority = null,
        Guid? courseId = null,
        Guid? parentTaskId = null,
        bool? overdue = null,
        bool includeInactiveCanvas = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tasks
            .AsNoTracking()
            .Include(task => task.Course)
            .Include(task => task.ParentTask)
            .AsQueryable();

        if (!includeInactiveCanvas)
        {
            query = query.Where(task => task.CanvasIsActive != false);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(task => task.Status.ToString() == status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(task => task.Priority.ToString() == priority);
        }

        if (courseId.HasValue)
        {
            query = query.Where(task => task.CourseId == courseId);
        }

        if (parentTaskId.HasValue)
        {
            query = query.Where(task => task.ParentTaskId == parentTaskId);
        }

        if (overdue.HasValue)
        {
            var now = DateTime.UtcNow;
            query = overdue.Value
                ? query.Where(task => task.DueDate < now && task.Status != TaskStatus.Completed)
                : query.Where(task => task.DueDate > now && task.Status != TaskStatus.Completed);
        }

        var tasks = await query.ToListAsync(cancellationToken);
        return Results.Ok(tasks.Select(task => task.ToRecord()));
    }

    private static async Task<IResult> GetTask(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(item => item.Course)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return task is null ? Results.NotFound() : Results.Ok(task.ToRecord());
    }

    private static async Task<IResult> CreateTask(
        CreateTaskCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Results.BadRequest("`title` is a required argument");
        }

        if (!Enum.TryParse(command.Priority, out TaskPriority priority))
        {
            return Results.BadRequest(
                "`priority` could not be correlated with a valid TaskPriority");
        }

        if (command.CourseId.HasValue &&
            !await db.Courses.AnyAsync(
                course => course.Id == command.CourseId,
                cancellationToken))
        {
            return Results.BadRequest("Specified course does not exist.");
        }

        if (command.ParentTaskId.HasValue &&
            !await db.Tasks.AnyAsync(
                task => task.Id == command.ParentTaskId,
                cancellationToken))
        {
            return Results.BadRequest("Specified parentTask does not exist.");
        }

        var now = DateTime.UtcNow;
        var task = new TaskEntity
        {
            Title = command.Title,
            Description = command.Description,
            DueDate = command.DueDate,
            Priority = priority,
            Status = TaskStatus.Todo,
            CourseId = command.CourseId,
            ParentTaskId = command.ParentTaskId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(task).Reference(item => item.Course).LoadAsync(cancellationToken);

        return Results.Created($"/internal/tasks/{task.Id}", task.ToRecord());
    }

    private static async Task<IResult> UpdateTask(
        [FromRoute] Guid id,
        UpdateTaskCommand command,
        AppDbContext db,
        TaskHierarchyService hierarchy,
        CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (task.CanvasAssignmentId.HasValue &&
            (command.NewTitle is not null ||
             command.UpdateDescription ||
             command.UpdateDueDate))
        {
            return Results.BadRequest(
                "Canvas-synced task titles, descriptions, and due dates cannot be updated.");
        }

        var hasNewPriority = Enum.TryParse(command.NewPriority, out TaskPriority priority);
        var hasNewStatus = Enum.TryParse(command.NewStatus, out TaskStatus status);

        task.Title = command.NewTitle ?? task.Title;
        task.Description = command.UpdateDescription ? command.NewDescription : task.Description;
        task.DueDate = command.UpdateDueDate ? command.NewDueDate : task.DueDate;
        task.Priority = hasNewPriority ? priority : task.Priority;
        task.Status = hasNewStatus ? status : task.Status;
        var updatedAt = DateTime.UtcNow;
        task.UpdatedAt = updatedAt;

        if (hasNewStatus && status == TaskStatus.Completed)
        {
            await hierarchy.CompleteDescendantsAsync(id, updatedAt, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(task.ToRecord());
    }

    private static async Task<IResult> DeleteTask(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> CreateSubtasks(
        [FromRoute] Guid id,
        CreateSubtasksCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(command.Priority, out TaskPriority priority))
        {
            return Results.BadRequest(
                "`priority` could not be correlated with a valid TaskPriority.");
        }

        var parent = await db.Tasks
            .Include(task => task.Course)
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (parent is null)
        {
            return Results.NotFound();
        }

        var now = DateTime.UtcNow;
        var tasks = command.Tasks.Select(item => new TaskEntity
        {
            Title = item.Title,
            Description = item.Description,
            DueDate = parent.DueDate,
            Priority = priority,
            Status = TaskStatus.Todo,
            CourseId = parent.CourseId,
            Course = parent.Course,
            ParentTaskId = parent.Id,
            ParentTask = parent,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/internal/tasks/{parent.Id}",
            tasks.Select(task => task.ToRecord()).ToList());
    }

    private static async Task<IResult> ApplyCanvasSnapshot(
        CanvasSnapshotCommand command,
        CanvasSnapshotService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.ApplyAsync(command, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> GetDueReminders(
        int hoursBeforeDue,
        int finalHoursBeforeDue,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tasks = await db.Tasks
            .AsNoTracking()
            .Include(task => task.Course)
            .Where(task => task.DueDate != null
                && task.Status != TaskStatus.Completed
                && task.CanvasIsActive != false
                && task.DueDate > now
                && task.DueDate <= now.AddHours(hoursBeforeDue))
            .ToListAsync(cancellationToken);

        var due = tasks
            .Where(task =>
            {
                var dueDate = task.DueDate!.Value;
                return task.DueSoonReminderSentAtUtc is null ||
                    (dueDate <= now.AddHours(finalHoursBeforeDue) &&
                     task.DueSoonReminderSentAtUtc < dueDate.AddHours(-finalHoursBeforeDue));
            })
            .Select(task => task.ToRecord())
            .ToList();

        return Results.Ok(due);
    }

    private static async Task<IResult> MarkReminderSent(
        [FromRoute] Guid id,
        MarkReminderSentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await db.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        task.DueSoonReminderSentAtUtc = command.SentAtUtc;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
