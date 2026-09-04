using GradesManager.Data;
using GradesManager.DTOs;
using GradesManager.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        private static async Task<IResult> GetStudent([FromRoute] Guid id, AppDbContext db)
        {
            var student = await db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentId == id);

            return student == null ? Results.NotFound() : Results.Ok(student.ToDto());
        }

        private static async Task<IResult> AddIdealMark(ModifyIdealMarkDto requestDto, AppDbContext db)
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

            var student = await db.Students
            .FindAsync(requestDto.StudentId);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (student.IdealMark.HasValue)
            {
                return Results.BadRequest("Ideal mark has already been set for this student.");
            }

            student.IdealMark = requestDto.idealMark.Value;
            await db.SaveChangesAsync();
            return Results.Ok(student.ToDto());
        }

        private static async Task<IResult> UpdateIdealMark(ModifyIdealMarkDto requestDto, AppDbContext db)
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

            var student = await db.Students
            .FindAsync(requestDto.StudentId);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (!student.IdealMark.HasValue)
            {
                return Results.BadRequest("Ideal mark has not been set for this student.");
            }

            student.IdealMark = requestDto.idealMark.Value;
            await db.SaveChangesAsync();
            return Results.Ok(student.ToDto());
        }

        private static async Task<IResult> DeleteIdealMark([FromRoute] Guid studentId, AppDbContext db)
        {
            var student = await db.Students
                .FindAsync(studentId);

            if (student is null)
            {
                return Results.NotFound();
            }

            if (student.IdealMark is null)
            {
                return Results.BadRequest("No ideal mark found for this student.");
            }
            student.IdealMark = null;
            await db.SaveChangesAsync();

            return Results.Ok();
        }

        private static async Task<IResult> GetStudents(
        AppDbContext db)
        {
            var query = db.Students.AsNoTracking();

            var students = await query.ToListAsync();

            var studentsDtos = students.Select(c => c.ToDto());

            return Results.Ok(studentsDtos);
        }
    }
}
