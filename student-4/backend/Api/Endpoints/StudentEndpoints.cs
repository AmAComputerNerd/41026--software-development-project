using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/students");

        group.MapGet("/", GetStudents);
        group.MapGet("/{userId:guid}", GetStudent);
        group.MapPut("/{userId:guid}", UpdateStudent);
        group.MapDelete("/{userId:guid}", DeleteStudent);

        return endpoints;
    }

    private static async Task<IResult> GetStudents(AppDbContext db)
    {
        var students = await db.Students
            .AsNoTracking()
            .Select(s => s.ToDto())
            .ToListAsync();

        return Results.Ok(students);
    }

    private static async Task<IResult> GetStudent(
        Guid userId,
        AppDbContext db)
    {
        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.ToDto())
            .FirstOrDefaultAsync();

        if (student is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(student);
    }

    private static async Task<IResult> UpdateStudent(
        Guid userId,
        [FromBody] UpdateStudentRequestDto request,
        AppDbContext db)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        // Upsert: if no Student record exists for this user yet, create
        // one. This makes the PUT endpoint usable as a "save my profile
        // details" call from the UI, regardless of whether the
        // record was created at sign-up time.
        if (student is null)
        {
            // Verify the user actually exists and is a Student before
            // we create a Student record for them.
            var userExists = await db.Users
                .AnyAsync(u => u.Id == userId && u.UserType == UserType.Student);
            if (!userExists)
            {
                return Results.NotFound("User not found or is not a Student.");
            }

            student = new Student(userId)
            {
                CourseStatus = request.CourseStatus ?? CourseStatus.FullTime,
                IsInternational = request.IsInternational ?? false,
                CanvasApiKey = request.CanvasApiKey ?? string.Empty,
            };
            db.Students.Add(student);
        }
        else
        {
            // Only overwrite fields that are provided (non-null)
            if (request.CourseStatus.HasValue)
                student.CourseStatus = request.CourseStatus.Value;
            if (request.IsInternational.HasValue)
                student.IsInternational = request.IsInternational.Value;
            if (request.CanvasApiKey is not null)
                student.CanvasApiKey = request.CanvasApiKey;
        }

        await db.SaveChangesAsync();

        return Results.Ok(student.ToDto());
    }

    private static async Task<IResult> DeleteStudent(
        Guid userId,
        AppDbContext db)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null)
        {
            return Results.NotFound();
        }

        db.Students.Remove(student);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}