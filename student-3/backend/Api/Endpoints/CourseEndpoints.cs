using Api.Data;
using Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

public static class CourseEndpoints
{
    // Open question: where are we storing courses? Relevant to a number of areas, could just store a reference database somewhere I suppose.
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/courses");

        group.MapGet("/", GetCourses);
        group.MapGet("/{id:guid}", GetCourse);
        
        return endpoints;
    }

    private static async Task<IResult> GetCourses(AppDbContext db)
    {
        var courses = await db.Courses
            .AsNoTracking()
            .ToListAsync();

        var courseDtos = courses.Select(c => c.ToDto());
        
        return Results.Ok(courseDtos);
    }

    private static async Task<IResult> GetCourse([FromRoute] Guid id, AppDbContext db)
    {
        var course = await db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
        
        return course == null ? Results.NotFound() : Results.Ok(course.ToDto());
    }
}