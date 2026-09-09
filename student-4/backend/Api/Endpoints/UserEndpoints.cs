using Api.DTOs;
using Api.Extensions;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Student4.Contracts;

namespace Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapGet("/", GetUsers);
        group.MapGet("/{userId:guid}", GetUser);
        group.MapPost("/", CreateUser);
        group.MapPut("/{userId:guid}", UpdateUser);
        group.MapDelete("/{userId:guid}", DeleteUser);

        return endpoints;
    }

    private static async Task<IResult> GetUsers(
        IAccountDatabaseClient db,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await db.GetUsersAsync(cancellationToken);
            return Results.Ok(users.Select(u => u.ToDto()));
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> GetUser(
        Guid userId,
        IAccountDatabaseClient db,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await db.GetUserAsync(userId, cancellationToken);
            return user is null ? Results.NotFound("No user found") : Results.Ok(user.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> CreateUser(
        IAccountDatabaseClient db,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateUserCommand(
                Email: request.Email,
                PasswordHash: request.PasswordHash,
                FirstName: request.FirstName,
                MiddleNames: request.MiddleNames,
                LastName: request.LastName,
                Gender: request.Gender,
                DateOfBirth: request.DateOfBirth,
                UserType: request.UserType,
                Student: request.StudentDto is null
                    ? null
                    : new CreateStudentCommand(
                        CourseStatus: request.StudentDto.CourseStatus,
                        IsInternational: request.StudentDto.IsInternational,
                        CanvasApiKey: request.StudentDto.CanvasApiKey),
                Teacher: request.TeacherDto is null
                    ? null
                    : new CreateTeacherCommand(
                        EmploymentStatus: request.TeacherDto.EmploymentStatus,
                        CanvasApiKey: request.TeacherDto.CanvasApiKey)
            );

            var user = await db.CreateUserAsync(command, cancellationToken);
            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: user.Id,
                    Type: "Account",
                    SourceMicroservice: "account",
                    Message: "Account created successfully.",
                    RelatedEntityType: "User",
                    RelatedEntityId: user.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("Api.Endpoints.UserEndpoints");
                UserEndpointsLog.PushNotificationFailed(logger, user.Id, ex);
            }

            return Results.Created($"/api/users/{user.Id}", user.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> UpdateUser(
        Guid userId,
        IAccountDatabaseClient db,
        INotificationClient notificationClient,
        ILoggerFactory loggerFactory,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateUserCommand(
                Email: request.Email,
                FirstName: request.FirstName,
                MiddleNames: request.MiddleNames,
                LastName: request.LastName,
                Gender: request.Gender,
                DateOfBirth: request.DateOfBirth,
                UserProfile: request.UserProfile
            );

            var user = await db.UpdateUserAsync(userId, command, cancellationToken);
            if (user is null)
            {
                return Results.NotFound("No User Found");
            }

            try
            {
                await notificationClient.PushAsync(new PushNotificationDto(
                    StudentId: user.Id,
                    Type: "Account",
                    SourceMicroservice: "account",
                    Message: "Profile updated successfully.",
                    RelatedEntityType: "User",
                    RelatedEntityId: user.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("Api.Endpoints.UserEndpoints");
                UserEndpointsLog.PushNotificationFailed(logger, user.Id, ex);
            }

            return Results.Ok(user.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> DeleteUser(
        Guid userId,
        IAccountDatabaseClient db,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await db.DeleteUserAsync(userId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound("No User Found");
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}

internal static partial class UserEndpointsLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to push notification for user {UserId}.")]
    public static partial void PushNotificationFailed(ILogger logger, Guid userId, Exception ex);
}
