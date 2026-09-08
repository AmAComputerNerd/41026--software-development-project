using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Api.Endpoints;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/teachers");

        group.MapGet("/{userId:guid}", GetTeacher);
        group.MapPut("/{userId:guid}", UpdateTeacher);

        return endpoints;
    }

    private static async Task<IResult> GetTeacher(
        Guid userId,
        IAccountDatabaseClient db,
        CancellationToken cancellationToken)
    {
        try
        {
            var teacher = await db.GetTeacherAsync(userId, cancellationToken);
            return teacher is null ? Results.NotFound() : Results.Ok(teacher.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> UpdateTeacher(
        Guid userId,
        IAccountDatabaseClient db,
        [FromBody] UpdateTeacherRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateTeacherCommand(
                EmploymentStatus: request.EmploymentStatus,
                CanvasApiKey: request.CanvasApiKey
            );

            var teacher = await db.UpdateTeacherAsync(userId, command, cancellationToken);
            return Results.Ok(teacher.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}
