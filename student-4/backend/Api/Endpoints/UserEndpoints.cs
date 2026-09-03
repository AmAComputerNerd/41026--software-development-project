using Api.Data;
using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        group.MapGet("/{userId:guid}/courses", GetUserCourses);
        group.MapPost(
            "/{userId:guid}/courses/{courseId:guid}",
            AddUserToCourse);
        group.MapDelete(
            "/{userId:guid}/courses/{courseId:guid}",
            RemoveUserFromCourse);

        return endpoints;
    }

    private static async Task<IResult> GetUsers(AppDbContext db)
    {
        var users = await db.Users
            .AsNoTracking()
            .Select(u => u.ToDto())
            .ToListAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUser(
        Guid userId,
        AppDbContext db)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.ToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(
        AppDbContext db,
        [FromBody] CreateUserRequestDto request)
    {
        bool emailExists = await db.Users
        .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            return Results.Conflict("A user with this email already exists.");

        User user = new User
        {
            Email = request.Email,
            PasswordHash = request.PasswordHash,
            FirstName = request.FirstName,
            MiddleNames = request.MiddleNames,
            LastName = request.LastName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            UserType = request.UserType,
        };

        db.Users.Add(user);

        switch (request.UserType)
        {
            case UserType.Student:
                if (request.StudentDto is null)
                    return Results.BadRequest("Student details are required.");

                db.Students.Add(new Student(user.Id)
                {
                    CourseStatus = request.StudentDto.CourseStatus,
                    IsInternational = request.StudentDto.IsInternational,
                    CanvasApiKey = request.StudentDto.CanvasApiKey
                });
                break;

            case UserType.Teacher:
                if (request.TeacherDto is null)
                    return Results.BadRequest("Teacher details are required.");

                db.Teachers.Add(new Teacher(user.Id)
                {
                    EmploymentStatus = request.TeacherDto.EmploymentStatus,
                    CanvasApiKey = request.TeacherDto.CanvasApiKey
                });
                break;

            case UserType.Admin:
                break;
        }

        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/users/{user.Id}",
            user.ToDto());
    }

    private static async Task<IResult> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequestDto request,
        AppDbContext db)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return Results.NotFound();
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.MiddleNames = request.MiddleNames;
        user.LastName = request.LastName;
        user.Gender = request.Gender;
        user.DateOfBirth = request.DateOfBirth;
        // UserProfile is nullable — if provided, update it; if null,
        // leave the existing value alone (don't clear it).
        if (request.UserProfile is not null)
        {
            user.UserProfile = request.UserProfile;
        }

        await db.SaveChangesAsync();

        return Results.Ok(user.ToDto());
    }

    private static async Task<IResult> DeleteUser(
        Guid userId,
        AppDbContext db)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return Results.NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetUserCourses(
        Guid userId,
        AppDbContext db)
    {
        var courses = await db.UserCourses
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            //.Select(uc => uc.Course.ToDto())
            .ToListAsync();

        return Results.Ok(courses);
    }

    private static async Task<IResult> AddUserToCourse(
        Guid userId,
        Guid courseId,
        AppDbContext db)
    {
        var userExists = await db.Users
            .AnyAsync(u => u.Id == userId);

        if (!userExists)
        {
            return Results.NotFound("User not found.");
        }

        var userCourseExists = await db.UserCourses
            .AnyAsync(uc =>
                uc.UserId == userId &&
                uc.CourseId == courseId);

        if (userCourseExists)
        {
            return Results.Conflict("User is already enrolled in this course.");
        }

        db.UserCourses.Add(new UserCourse(userId, courseId));

        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveUserFromCourse(
        Guid userId,
        Guid courseId,
        AppDbContext db)
    {
        var userCourse = await db.UserCourses
            .FirstOrDefaultAsync(uc =>
                uc.UserId == userId &&
                uc.CourseId == courseId);

        if (userCourse is null)
        {
            return Results.NotFound();
        }

        db.UserCourses.Remove(userCourse);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}