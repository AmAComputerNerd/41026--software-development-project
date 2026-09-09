using GradesManager.DTOs;
using GradesManager.Services;
using Microsoft.AspNetCore.Mvc;

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
            IDatabaseClient databaseClient,
            bool includeInactiveCanvas = false,
            CancellationToken cancellationToken = default)
        {
            var courses = await databaseClient.GetCoursesAsync(includeInactiveCanvas, cancellationToken);

            if (courses == null)
            {
                return Results.NotFound();
            }

            var courseDtos = courses.Select(c => new CourseDto(
                c.CourseId, c.Code, c.Name, c.CanvasCourseId, c.CanvasIsActive, c.LastCanvasSyncAt));

            return Results.Ok(courseDtos);
        }

        private static async Task<IResult> GetCourse(
            [FromRoute] Guid id,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            var course = await databaseClient.GetCourseAsync(id, cancellationToken);

            return course == null
                ? Results.NotFound()
                : Results.Ok(new CourseDto(
                    course.CourseId, course.Code, course.Name, course.CanvasCourseId, course.CanvasIsActive, course.LastCanvasSyncAt));
        }
    }
}
