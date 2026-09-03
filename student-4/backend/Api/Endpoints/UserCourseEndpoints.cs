using Api.Data;
using Api.Extensions;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class UserCourseEndpoints
{
    public static IEndpointRouteBuilder MapUserCourseEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/user-courses");

        group.MapGet("/", GetUserCourses);
        group.MapGet("/{userId:guid}", GetCoursesForUser);
        group.MapPost("/", AddUserCourse);
        group.MapDelete("/{userId:guid}/{courseId:guid}", RemoveUserCourse);

        return endpoints;
    }

    private static async Task<IResult> GetUserCourses(
        AppDbContext db)
    {
        var userCourses = await db.UserCourses
            .AsNoTracking()
            .Select(uc => uc.ToDto())
            .ToListAsync();

        return Results.Ok(userCourses);
    }

    private static async Task<IResult> GetCoursesForUser(
        Guid userId,
        AppDbContext db)
    {
        var userExists = await db.Users
            .AnyAsync(u => u.Id == userId);

        if (!userExists)
        {
            return Results.NotFound("User not found.");
        }

        var courses = await db.UserCourses
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            //.Select(uc => uc.Course.ToDto())
            .ToListAsync();

        return Results.Ok(courses);
    }

    private static async Task<IResult> AddUserCourse(
        UserCourse userCourse,
        AppDbContext db)
    {
        var userExists = await db.Users
            .AnyAsync(u => u.Id == userCourse.UserId);

        if (!userExists)
        {
            return Results.NotFound("User not found.");
        }

        /*var courseExists = await db.Courses
            .AnyAsync(c => c.Id == userCourse.CourseId);

        if (!courseExists)
        {
            return Results.NotFound("Course not found.");
        }*/

        var userCourseExists = await db.UserCourses
            .AnyAsync(uc =>
                uc.UserId == userCourse.UserId &&
                uc.CourseId == userCourse.CourseId);

        if (userCourseExists)
        {
            return Results.Conflict(
                "User is already enrolled in this course.");
        }

        db.UserCourses.Add(userCourse);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/user-courses/{userCourse.UserId}/{userCourse.CourseId}",
            userCourse.ToDto());
    }

    private static async Task<IResult> RemoveUserCourse(
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
