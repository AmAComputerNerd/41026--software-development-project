using GradesManager.Data;
using GradesManager.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints
{
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
        AppDbContext db,
        bool includeInactiveCanvas = false)
        {
            var query = db.Courses.AsNoTracking();
            if (!includeInactiveCanvas)
            {
                query = query.Where(course => course.CanvasIsActive != false);
            }

            var courses = await query.ToListAsync();

            var courseDtos = courses.Select(c => c.ToDto());

            return Results.Ok(courseDtos);
        }

        private static async Task<IResult> GetCourse([FromRoute] Guid id, AppDbContext db)
        {
            var course = await db.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CourseId == id);

            return course == null ? Results.NotFound() : Results.Ok(course.ToDto());
        }
    }
}
