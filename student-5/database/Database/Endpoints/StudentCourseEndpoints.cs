using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class StudentCourseEndpoints
{
    public static IEndpointRouteBuilder MapStudentCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/student-courses");

        group.MapGet("/", GetStudentCourses);
        group.MapGet("/student/{studentId:guid}", GetStudentCoursesByStudent);
        group.MapPost("/", CreateStudentCourse);
        group.MapDelete("/{studentId:guid}/{courseId:guid}", DeleteStudentCourse);

        return endpoints;
    }

    private static async Task<IResult> GetStudentCourses(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var studentCourses = await db.StudentCourses.AsNoTracking().ToListAsync(cancellationToken);
        var records = studentCourses.Select(sc => new StudentCourseRecord(
            sc.StudentId, sc.CourseId));
        return Results.Ok(records);
    }

    private static async Task<IResult> GetStudentCoursesByStudent(
        [FromRoute] Guid studentId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            return Results.BadRequest("Student ID cannot be empty.");
        }

        var student = await db.Students.FindAsync([studentId], cancellationToken);
        if (student == null)
        {
            return Results.NotFound();
        }

        var studentCourses = await db.StudentCourses
            .Where(sc => sc.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var records = studentCourses.Select(sc => new StudentCourseRecord(
            sc.StudentId, sc.CourseId));
        return Results.Ok(records);
    }

    private static async Task<IResult> CreateStudentCourse(
        CreateStudentCourseCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([command.StudentId], cancellationToken);
        if (student == null)
        {
            return Results.NotFound("Student not found.");
        }

        var course = await db.Courses.FindAsync([command.CourseId], cancellationToken);
        if (course == null)
        {
            return Results.NotFound("Course not found.");
        }

        var existing = await db.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == command.StudentId && sc.CourseId == command.CourseId, cancellationToken);

        if (existing != null)
        {
            return Results.BadRequest("Student course already exists.");
        }

        var studentCourse = new StudentCourse
        {
            StudentId = command.StudentId,
            CourseId = command.CourseId
        };

        db.StudentCourses.Add(studentCourse);
        await db.SaveChangesAsync(cancellationToken);

        var record = new StudentCourseRecord(studentCourse.StudentId, studentCourse.CourseId);
        return Results.Created($"/internal/student-courses/{studentCourse.StudentId}/{studentCourse.CourseId}", record);
    }

    private static async Task<IResult> DeleteStudentCourse(
        [FromRoute] Guid studentId,
        [FromRoute] Guid courseId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty || courseId == Guid.Empty)
        {
            return Results.BadRequest("Student ID and Course ID cannot be empty.");
        }

        var studentCourse = await db.StudentCourses
            .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId, cancellationToken);

        if (studentCourse == null)
        {
            return Results.NotFound();
        }

        db.StudentCourses.Remove(studentCourse);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}