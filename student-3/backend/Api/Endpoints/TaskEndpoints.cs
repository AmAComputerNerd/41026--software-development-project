using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
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
            var now = DateTimeOffset.UtcNow;
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
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
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

    private static async Task<IResult> UpdateTask([FromRoute] Guid id, ModifyTaskRequestDto requestDto, AppDbContext db)
    {
        var task = await db.Tasks
            .FindAsync(id);

        if (task is null)
        {
            return Results.NotFound();
        }

        var hasNewPriority = Enum.TryParse(requestDto.NewPriority, out TaskPriority newPriority);
        var hasNewStatus = Enum.TryParse(requestDto.NewStatus, out TaskStatus newStatus);

        task.Title = requestDto.NewTitle ?? task.Title;
        task.Description = requestDto.UpdateDescription ? requestDto.NewDescription : task.Description;
        task.DueDate = requestDto.NewDueDate ?? task.DueDate;
        task.Priority = hasNewPriority ? newPriority : task.Priority;
        task.Status = hasNewStatus ? newStatus : task.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        
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