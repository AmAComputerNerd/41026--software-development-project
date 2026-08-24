using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class CanvasEndpoints
{
    public static IEndpointRouteBuilder MapCanvasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/canvas");
        group.MapGet("/courses", GetCourses);
        group.MapGet("/courses/{courseId:long}/assignments", GetAssignments);
        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var courses = await canvas.GetCoursesAsync(cancellationToken);
        return Results.Ok(courses);
    }

    private static async Task<IResult> GetAssignments(
        [FromRoute] long courseId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var assignments = await canvas.GetAssignmentsAsync(courseId, cancellationToken);
        return Results.Ok(assignments);
    }
}
