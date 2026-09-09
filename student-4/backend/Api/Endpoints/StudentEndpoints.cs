using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/students");

        group.MapGet("/{userId:guid}", GetStudent);
        group.MapPut("/{userId:guid}", UpdateStudent);

        return endpoints;
    }

    private static async Task<IResult> GetStudent(
        Guid userId,
        IAccountDatabaseClient db,
        CancellationToken cancellationToken)
    {
        try
        {
            var student = await db.GetStudentAsync(userId, cancellationToken);
            return student is null ? Results.NotFound() : Results.Ok(student.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> UpdateStudent(
        Guid userId,
        IAccountDatabaseClient db,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        [FromBody] UpdateStudentRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateStudentCommand(
                CourseStatus: request.CourseStatus,
                IsInternational: request.IsInternational,
                CanvasApiKey: request.CanvasApiKey
            );

            var student = await db.UpdateStudentAsync(userId, command, cancellationToken);

            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: userId,
                    Type: "Account",
                    SourceMicroservice: "account",
                    Message: "Student profile updated successfully.",
                    RelatedEntityType: "User",
                    RelatedEntityId: userId), cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("Api.Endpoints.StudentEndpoints");
                StudentEndpointsLog.PushNotificationFailed(logger, userId, ex);
            }

            return Results.Ok(student.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}

internal static partial class StudentEndpointsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for student {UserId}.")]
    public static partial void PushNotificationFailed(ILogger logger, Guid userId, Exception ex);
}
