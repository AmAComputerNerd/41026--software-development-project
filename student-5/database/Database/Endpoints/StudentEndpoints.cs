using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/students");

        group.MapGet("/", GetStudents);
        group.MapGet("/{id:guid}", GetStudent);
        group.MapPost("/", CreateStudent);
        group.MapPut("/{id:guid}", UpdateStudent);
        group.MapDelete("/{id:guid}", DeleteStudent);

        return endpoints;
    }

    private static async Task<IResult> GetStudents(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var students = await db.Students.AsNoTracking().ToListAsync(cancellationToken);
        var records = students.Select(s => new StudentRecord(s.StudentId, s.Name, s.IdealMark));
        return Results.Ok(records);
    }

    private static async Task<IResult> GetStudent(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == id, cancellationToken);

        return student == null
            ? Results.NotFound()
            : Results.Ok(new StudentRecord(student.StudentId, student.Name, student.IdealMark));
    }

    private static async Task<IResult> CreateStudent(
        CreateStudentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var student = new Student
        {
            Name = command.Name,
            IdealMark = command.IdealMark
        };

        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);

        var record = new StudentRecord(student.StudentId, student.Name, student.IdealMark);
        return Results.Created($"/internal/students/{student.StudentId}", record);
    }

    private static async Task<IResult> UpdateStudent(
        [FromRoute] Guid id,
        UpdateStudentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (id != command.StudentId)
        {
            return Results.BadRequest("Student ID mismatch.");
        }

        var student = await db.Students.FindAsync([id], cancellationToken);
        if (student == null)
        {
            return Results.NotFound();
        }

        student.Name = command.Name;
        student.IdealMark = command.IdealMark;

        await db.SaveChangesAsync(cancellationToken);

        var record = new StudentRecord(student.StudentId, student.Name, student.IdealMark);
        return Results.Ok(record);
    }

    private static async Task<IResult> DeleteStudent(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var student = await db.Students.FindAsync([id], cancellationToken);
        if (student == null)
        {
            return Results.NotFound();
        }

        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}