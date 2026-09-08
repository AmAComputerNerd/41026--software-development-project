using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/courses");

        group.MapGet("/", GetCourses);
        group.MapGet("/{id:guid}", GetCourse);
        group.MapPost("/", CreateCourse);
        group.MapPut("/{id:guid}", UpdateCourse);
        group.MapDelete("/{id:guid}", DeleteCourse);

        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        [AsParameters] GetCoursesQuery query,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var q = db.Courses.AsNoTracking();
        if (!query.IncludeInactiveCanvas)
        {
            q = q.Where(c => c.CanvasIsActive != false);
        }

        var courses = await q.ToListAsync(cancellationToken);
        var records = courses.Select(c => new CourseRecord(
            c.CourseId, c.Code, c.Name, c.CanvasCourseId, c.CanvasIsActive, c.LastCanvasSyncAt));
        return Results.Ok(records);
    }

    private static async Task<IResult> GetCourse(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var course = await db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseId == id, cancellationToken);

        return course == null
            ? Results.NotFound()
            : Results.Ok(new CourseRecord(
                course.CourseId, course.Code, course.Name, course.CanvasCourseId, course.CanvasIsActive, course.LastCanvasSyncAt));
    }

    private static async Task<IResult> CreateCourse(
        CreateCourseCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Code = command.Code,
            Name = command.Name,
            CanvasCourseId = command.CanvasCourseId,
            CanvasIsActive = command.CanvasIsActive,
            LastCanvasSyncAt = command.LastCanvasSyncAt
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        var record = new CourseRecord(
            course.CourseId, course.Code, course.Name, course.CanvasCourseId, course.CanvasIsActive, course.LastCanvasSyncAt);
        return Results.Created($"/internal/courses/{course.CourseId}", record);
    }

    private static async Task<IResult> UpdateCourse(
        [FromRoute] Guid id,
        UpdateCourseCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (id != command.CourseId)
        {
            return Results.BadRequest("Course ID mismatch.");
        }

        var course = await db.Courses.FindAsync([id], cancellationToken);
        if (course == null)
        {
            return Results.NotFound();
        }

        course.Code = command.Code;
        course.Name = command.Name;
        course.CanvasCourseId = command.CanvasCourseId;
        course.CanvasIsActive = command.CanvasIsActive;
        course.LastCanvasSyncAt = command.LastCanvasSyncAt;

        await db.SaveChangesAsync(cancellationToken);

        var record = new CourseRecord(
            course.CourseId, course.Code, course.Name, course.CanvasCourseId, course.CanvasIsActive, course.LastCanvasSyncAt);
        return Results.Ok(record);
    }

    private static async Task<IResult> DeleteCourse(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var course = await db.Courses.FindAsync([id], cancellationToken);
        if (course == null)
        {
            return Results.NotFound();
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}