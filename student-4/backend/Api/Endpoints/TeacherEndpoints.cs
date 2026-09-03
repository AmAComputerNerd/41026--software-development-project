using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teachers");

        group.MapGet("/", GetTeachers);
        group.MapGet("/{userId:guid}", GetTeacher);
        group.MapPut("/{userId:guid}", UpdateTeacher);
        group.MapDelete("/{userId:guid}", DeleteTeacher);

        return endpoints;
    }

    private static async Task<IResult> GetTeachers(AppDbContext db)
    {
        var teachers = await db.Teachers
            .AsNoTracking()
            .Select(t => t.ToDto())
            .ToListAsync();

        return Results.Ok(teachers);
    }

    private static async Task<IResult> GetTeacher(
        Guid userId,
        AppDbContext db)
    {
        var teacher = await db.Teachers
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.ToDto())
            .FirstOrDefaultAsync();

        if (teacher is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(teacher);
    }

    private static async Task<IResult> UpdateTeacher(
        Guid userId,
        [FromBody] UpdateTeacherRequestDto request,
        AppDbContext db)
    {
        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        // Upsert: if no Teacher record exists for this user yet, create
        // one. This makes the PUT endpoint usable as a "save my profile
        // details" call from the UI, regardless of whether the
        // record was created at sign-up time.
        if (teacher is null)
        {
            // Verify the user actually exists and is a Teacher before
            // we create a Teacher record for them.
            var userExists = await db.Users
                .AnyAsync(u => u.Id == userId && u.UserType == UserType.Teacher);
            if (!userExists)
            {
                return Results.NotFound("User not found or is not a Teacher.");
            }

            teacher = new Teacher(userId)
            {
                EmploymentStatus = request.EmploymentStatus ?? EmploymentStatus.FullTime,
                CanvasApiKey = request.CanvasApiKey ?? string.Empty,
            };
            db.Teachers.Add(teacher);
        }
        else
        {
            // Only overwrite fields that are provided (non-null)
            if (request.EmploymentStatus.HasValue)
                teacher.EmploymentStatus = request.EmploymentStatus.Value;
            if (request.CanvasApiKey is not null)
                teacher.CanvasApiKey = request.CanvasApiKey;
        }

        await db.SaveChangesAsync();

        return Results.Ok(teacher.ToDto());
    }

    private static async Task<IResult> DeleteTeacher(
        Guid userId,
        AppDbContext db)
    {
        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher is null)
        {
            return Results.NotFound();
        }

        db.Teachers.Remove(teacher);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}