using GradesManager.Data;
using GradesManager.DTOs;
using GradesManager.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints
{
    public static class AssignmentEndpoints
    {
        public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/assignment");

            group.MapGet("/{id:guid}", GetAssignment);
            group.MapGet("/student/{studentId:guid}", GetAssignmentsByStudent);
            group.MapGet("/course/{courseId:guid}", GetAssignmentsByCourse);
            group.MapPost("/marks/", AddTempMark);
            group.MapPut("/marks/", UpdateTempMark);
            group.MapGet("/marks/{studentId:guid}", GetStudentMarks);

            return endpoints;
        }

        private static async Task<IResult> GetAssignment([FromRoute] Guid id, AppDbContext db)
        {
            var assignment = await db.Assignments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssignmentId == id);
            return assignment == null ? Results.NotFound() : Results.Ok(assignment.ToDto());
        }

        private static async Task<IResult> GetAssignmentsByStudent([FromRoute] Guid studentId, AppDbContext db)
        {
            if (studentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }

            var student = await db.Students
            .FindAsync(studentId);

            if (student is null)
            {
                return Results.NotFound();
            }

            var assignments = await db.StudentAssignments
                .Where(sa => sa.StudentId == studentId)
                .Select(sa => sa.Assignment)
                .ToListAsync();

            return Results.Ok(assignments.Select(a => a.ToDto()));

        }

        private static async Task<IResult> GetAssignmentsByCourse([FromRoute] Guid courseId, AppDbContext db)
        {
            if (courseId == Guid.Empty)
            {
                return Results.BadRequest("Course ID cannot be empty.");
            }

            var course = await db.Courses
            .FindAsync(courseId);

            if (course is null)
            {
                return Results.NotFound();
            }

            var assignments = await db.Assignments
                .Where(a => a.CourseId == courseId)
                .ToListAsync();

            return Results.Ok(assignments.Select(a => a.ToDto()));

        }

        private static async Task<IResult> GetStudentMarks([FromRoute] Guid studentId, AppDbContext db)
        {
            if (studentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }
            var student = await db.Students
            .FindAsync(studentId);
            if (student is null)
            {
                return Results.NotFound();
            }
            var studentAssignments = await db.StudentAssignments
                .Where(sa => sa.StudentId == studentId)
                .ToListAsync();
            return Results.Ok(studentAssignments.Select(sa => sa.ToDto()));
        }

        private static async Task<IResult> AddTempMark(ModifyTempMarkDto modifyTempMarkDto, AppDbContext db)
        {
            if (modifyTempMarkDto.StudentId == Guid.Empty || modifyTempMarkDto.AssignmentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
            }
            var studentAssignment = await db.StudentAssignments
                .FirstOrDefaultAsync(sa => sa.StudentId == modifyTempMarkDto.StudentId && sa.AssignmentId == modifyTempMarkDto.AssignmentId);
            if (studentAssignment is null)
            {
                return Results.NotFound();
            }

            if (studentAssignment.TempMark.HasValue)
            {
                return Results.BadRequest("Temporary mark already exists. Use the update endpoint to modify it.");
            }

            studentAssignment.TempMark = modifyTempMarkDto.TempMark;
            await db.SaveChangesAsync();
            return Results.Ok(studentAssignment.ToDto());
        }

        private static async Task<IResult> UpdateTempMark(ModifyTempMarkDto modifyTempMarkDto, AppDbContext db)
        {
            if (modifyTempMarkDto.StudentId == Guid.Empty || modifyTempMarkDto.AssignmentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
            }
            var studentAssignment = await db.StudentAssignments
                .FirstOrDefaultAsync(sa => sa.StudentId == modifyTempMarkDto.StudentId && sa.AssignmentId == modifyTempMarkDto.AssignmentId);
            if (studentAssignment is null)
            {
                return Results.NotFound();
            }

            if (!studentAssignment.TempMark.HasValue)
            {
                return Results.BadRequest("No temporary mark exists to update.");
            }

            studentAssignment.TempMark = modifyTempMarkDto.TempMark;
            await db.SaveChangesAsync();
            return Results.Ok(studentAssignment.ToDto());
        }

    }
}
