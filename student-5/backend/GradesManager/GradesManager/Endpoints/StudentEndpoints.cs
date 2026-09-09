using GradesManager.Contracts;
using GradesManager.DTOs;
using GradesManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace GradesManager.Endpoints
{
    public static class StudentEndpoints
    {
        public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/students");

            group.MapGet("/", GetStudents);
            group.MapGet("/{id:guid}", GetStudent);
            group.MapPost("/", AddIdealMark);
            group.MapPut("/", UpdateIdealMark);
            group.MapDelete("/{studentId:guid}", DeleteIdealMark);

            return endpoints;
        }

        private static async Task<IResult> GetStudents(
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            var students = await databaseClient.GetStudentsAsync(cancellationToken);

            if (students == null)
            {
                return Results.NotFound();
            }

            var studentsDtos = students.Select(s => new StudentDto(s.StudentId, s.Name, s.IdealMark));
            return Results.Ok(studentsDtos);
        }

        private static async Task<IResult> GetStudent(
            [FromRoute] Guid id,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            var student = await databaseClient.GetStudentAsync(id, cancellationToken);

            return student == null
                ? Results.NotFound()
                : Results.Ok(new StudentDto(student.StudentId, student.Name, student.IdealMark));
        }

        private static async Task<IResult> AddIdealMark(
            ModifyIdealMarkDto requestDto,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (requestDto.idealMark is null)
            {
                return Results.BadRequest("Ideal mark cannot be null.");
            }

            if (requestDto.idealMark < 0 || requestDto.idealMark > 100)
            {
                return Results.BadRequest("Ideal mark must be between 0 and 100.");
            }

            if (requestDto.StudentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }

            var student = await databaseClient.GetStudentAsync(requestDto.StudentId, cancellationToken);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (student.IdealMark.HasValue)
            {
                return Results.BadRequest("Ideal mark has already been set for this student.");
            }

            var updatedStudent = await databaseClient.UpdateStudentAsync(requestDto.StudentId, new UpdateStudentCommand(
                requestDto.StudentId, student.Name, requestDto.idealMark.Value), cancellationToken);

            return updatedStudent == null
                ? Results.NotFound()
                : Results.Ok(new StudentDto(updatedStudent.StudentId, updatedStudent.Name, updatedStudent.IdealMark));
        }

        private static async Task<IResult> UpdateIdealMark(
            ModifyIdealMarkDto requestDto,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            if (requestDto.idealMark is null)
            {
                return Results.BadRequest("Ideal mark cannot be null.");
            }

            if (requestDto.idealMark < 0 || requestDto.idealMark > 100)
            {
                return Results.BadRequest("Ideal mark must be between 0 and 100.");
            }

            if (requestDto.StudentId == Guid.Empty)
            {
                return Results.BadRequest("Student ID cannot be empty.");
            }

            var student = await databaseClient.GetStudentAsync(requestDto.StudentId, cancellationToken);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (!student.IdealMark.HasValue)
            {
                return Results.BadRequest("Ideal mark has not been set for this student.");
            }

            var updatedStudent = await databaseClient.UpdateStudentAsync(requestDto.StudentId, new UpdateStudentCommand(
                requestDto.StudentId, student.Name, requestDto.idealMark.Value), cancellationToken);

            return updatedStudent == null
                ? Results.NotFound()
                : Results.Ok(new StudentDto(updatedStudent.StudentId, updatedStudent.Name, updatedStudent.IdealMark));
        }

        private static async Task<IResult> DeleteIdealMark(
            [FromRoute] Guid studentId,
            IDatabaseClient databaseClient,
            CancellationToken cancellationToken = default)
        {
            var student = await databaseClient.GetStudentAsync(studentId, cancellationToken);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (student.IdealMark is null)
            {
                return Results.BadRequest("No ideal mark found for this student.");
            }

            var updatedStudent = await databaseClient.UpdateStudentAsync(studentId, new UpdateStudentCommand(
                studentId, student.Name, null), cancellationToken);

            return updatedStudent == null
                ? Results.NotFound()
                : Results.Ok();
        }
    }
}
