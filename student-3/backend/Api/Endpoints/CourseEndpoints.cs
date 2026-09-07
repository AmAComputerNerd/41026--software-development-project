using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/courses");
        group.MapGet("/", GetCourses);
        group.MapGet("/{id:guid}", GetCourse);
        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        IStudent3DatabaseClient database,
        bool includeInactiveCanvas = false,
        CancellationToken cancellationToken = default)
    {
        var courses = await database.GetCoursesAsync(
            includeInactiveCanvas,
            cancellationToken);
        return Results.Ok(courses.Select(course => course.ToDto()));
    }

    private static async Task<IResult> GetCourse(
        [FromRoute] Guid id,
        IStudent3DatabaseClient database,
        CancellationToken cancellationToken)
    {
        var course = await database.GetCourseAsync(id, cancellationToken);
        return course is null ? Results.NotFound() : Results.Ok(course.ToDto());
    }
}
