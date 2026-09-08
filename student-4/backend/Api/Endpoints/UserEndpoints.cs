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
            return user is null ? Results.NotFound() : Results.Ok(user.ToDto());
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }

    private static async Task<IResult> CreateUser(
        IAccountDatabaseClient db,
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
            return user is null ? Results.NotFound() : Results.Ok(user.ToDto());
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
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}
