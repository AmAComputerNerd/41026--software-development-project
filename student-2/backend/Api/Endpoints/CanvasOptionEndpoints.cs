using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class CanvasOptionEndpoints
{
    public static IEndpointRouteBuilder MapCanvasOptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/canvas");
        group.MapGet("/courses", GetCourses);
        group.MapGet("/courses/{courseId:long}/recipients", GetRecipients);
        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        ISharedCanvasClient canvas,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await canvas.GetCoursesAsync(cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The shared Canvas service request failed.");
        }
    }

    private static async Task<IResult> GetRecipients(
        [FromRoute] long courseId,
        ISharedCanvasClient canvas,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await canvas.GetRecipientsAsync(courseId, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The shared Canvas service request failed.");
        }
    }
}