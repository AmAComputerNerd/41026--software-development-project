using GradesManager.Contracts;
using GradesManager.DTOs;
using GradesManager.Services;
using Microsoft.AspNetCore.Mvc;

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
            group.MapDelete("/marks/{studentId:guid}/{assignmentId:guid}", DeleteTempMark);
            group.MapGet("/marks/{studentId:guid}", GetStudentMarks);

            return endpoints;
        }

        private static async Task<IResult> GetAssignment(
            [FromRoute] Guid id,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            var assignment = await databaseClient.GetAssignmentAsync(id, cancellationToken);

            return assignment == null
                ? Results.NotFound()
                : Results.Ok(new AssignmentDto(
                    assignment.AssignmentId, assignment.CourseId, assignment.Name, assignment.Weight, assignment.MaxMark, assignment.Completed));
        }

        private static async Task<IResult> GetAssignmentsByStudent(
            [FromRoute] Guid studentId,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (studentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }

            var assignments = await databaseClient.GetAssignmentsByStudentAsync(studentId, cancellationToken);

            if (assignments == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(assignments.Select(a => new AssignmentDto(
                a.AssignmentId, a.CourseId, a.Name, a.Weight, a.MaxMark, a.Completed)));
        }

        private static async Task<IResult> GetAssignmentsByCourse(
            [FromRoute] Guid courseId,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (courseId == Guid.Empty)
            {
                return Results.BadRequest("Course ID cannot be empty.");
            }

            var assignments = await databaseClient.GetAssignmentsByCourseAsync(courseId, cancellationToken);

            if (assignments == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(assignments.Select(a => new AssignmentDto(
                a.AssignmentId, a.CourseId, a.Name, a.Weight, a.MaxMark, a.Completed)));
        }

        private static async Task<IResult> GetStudentMarks(
            [FromRoute] Guid studentId,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (studentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }

            var studentAssignments = await databaseClient.GetStudentMarksAsync(studentId, cancellationToken);

            if (studentAssignments == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(studentAssignments.Select(sa => new StudentAssignmentDto(
                sa.StudentId, sa.AssignmentId, sa.TempMark, sa.FinalMark)));
        }

        private static async Task<IResult> AddTempMark(
            ModifyTempMarkDto modifyTempMarkDto,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (modifyTempMarkDto.StudentId == Guid.Empty || modifyTempMarkDto.AssignmentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
            }

            var studentAssignments = await databaseClient.GetStudentMarksAsync(modifyTempMarkDto.StudentId, cancellationToken);
            var existing = studentAssignments?.FirstOrDefault(sa => sa.AssignmentId == modifyTempMarkDto.AssignmentId);

            if (existing == null)
            {
                return Results.NotFound();
            }

            if (existing.TempMark.HasValue)
            {
                return Results.BadRequest("Temporary mark already exists. Use the update endpoint to modify it.");
            }

            var created = await databaseClient.CreateStudentAssignmentAsync(new CreateStudentAssignmentCommand(
                modifyTempMarkDto.StudentId, modifyTempMarkDto.AssignmentId, modifyTempMarkDto.TempMark, null), cancellationToken);

            return created == null
                ? Results.NotFound()
                : Results.Ok(new StudentAssignmentDto(created.StudentId, created.AssignmentId, created.TempMark, created.FinalMark));
        }

        private static async Task<IResult> UpdateTempMark(
            ModifyTempMarkDto modifyTempMarkDto,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (modifyTempMarkDto.StudentId == Guid.Empty || modifyTempMarkDto.AssignmentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
            }

            var studentAssignments = await databaseClient.GetStudentMarksAsync(modifyTempMarkDto.StudentId, cancellationToken);
            var existing = studentAssignments?.FirstOrDefault(sa => sa.AssignmentId == modifyTempMarkDto.AssignmentId);

            if (existing == null)
            {
                return Results.NotFound();
            }

            if (!existing.TempMark.HasValue)
            {
                return Results.BadRequest("No temporary mark exists to update.");
            }

            var updated = await databaseClient.UpdateStudentAssignmentAsync(new UpdateStudentAssignmentCommand(
                modifyTempMarkDto.StudentId, modifyTempMarkDto.AssignmentId, modifyTempMarkDto.TempMark, existing.FinalMark), cancellationToken);

            return updated == null
                ? Results.NotFound()
                : Results.Ok(new StudentAssignmentDto(updated.StudentId, updated.AssignmentId, updated.TempMark, updated.FinalMark));
        }

        private static async Task<IResult> DeleteTempMark(
            [FromRoute] Guid studentId,
            [FromRoute] Guid assignmentId,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (studentId == Guid.Empty || assignmentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
            }

            var studentAssignments = await databaseClient.GetStudentMarksAsync(studentId, cancellationToken);
            var existing = studentAssignments?.FirstOrDefault(sa => sa.AssignmentId == assignmentId);

            if (existing == null)
            {
                return Results.NotFound();
            }

            if (!existing.TempMark.HasValue)
            {
                return Results.BadRequest("No temporary mark exists to delete.");
            }

            var deleted = await databaseClient.DeleteStudentAssignmentAsync(studentId, assignmentId, cancellationToken);

            return deleted ? Results.NoContent() : Results.NotFound();
        }
    }
}
