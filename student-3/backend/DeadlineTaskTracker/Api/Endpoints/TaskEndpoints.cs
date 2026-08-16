using Api.Data;
using Api.DTOs;
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
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (filter.CourseId.HasValue)
        {
            query = query.Where(t => t.CourseId == filter.CourseId.Value);
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

        if (requestDto.CourseId.HasValue)
        {
            var courseExists = await db.Courses.AnyAsync(c => c.Id == requestDto.CourseId.Value);
            if (!courseExists)
            {
                return Results.BadRequest("Specified course does not exist.");
            }
        }
        
        var task = new TaskEntity
        {
            Title = requestDto.Title,
            Description = requestDto.Description,
            DueDate = requestDto.DueDate,
            Priority = requestDto.Priority,
            Status = TaskStatus.Todo,
            CourseId = requestDto.CourseId,
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

        if (requestDto.NewCourseId.HasValue)
        {
            var courseExists = await db.Courses
                .AnyAsync(c => c.Id == requestDto.NewCourseId.Value);

            if (!courseExists)
            {
                return Results.BadRequest("Specified course does not exist.");
            }
        }

        if (requestDto.NewTitle?.Trim()?.Length == 0)
        {
            return Results.BadRequest("`title` is a required argument");
        }

        task.Title = requestDto.NewTitle ?? task.Title;
        task.Description = requestDto.UpdateDescription == true ? requestDto.NewDescription : task.Description;
        task.DueDate = requestDto.NewDueDate ?? task.DueDate;
        task.Priority = requestDto.NewPriority ?? task.Priority;
        task.Status = requestDto.NewStatus ?? task.Status;
        task.CourseId = requestDto.UpdateCourseId == true ? requestDto.NewCourseId : task.CourseId;
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