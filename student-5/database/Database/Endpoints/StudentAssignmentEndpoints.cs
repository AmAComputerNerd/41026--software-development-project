using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class StudentAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapStudentAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/student-assignments");

        group.MapGet("/student/{studentId:guid}", GetStudentMarks);
        group.MapPost("/", CreateStudentAssignment);
        group.MapPut("/", UpdateStudentAssignment);
        group.MapDelete("/{studentId:guid}/{assignmentId:guid}", DeleteStudentAssignment);

        return endpoints;
    }

    private static async Task<IResult> GetStudentMarks(
        [FromRoute] Guid studentId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty)
        {
            return Results.BadRequest("Student ID cannot be empty.");
        }

        var student = await db.Students.FindAsync([studentId], cancellationToken);
        if (student == null)
        {
            return Results.NotFound();
        }

        var studentAssignments = await db.StudentAssignments
            .Where(sa => sa.StudentId == studentId)
            .ToListAsync(cancellationToken);

        var records = studentAssignments.Select(sa => new StudentAssignmentRecord(
            sa.StudentId, sa.AssignmentId, sa.TempMark, sa.FinalMark));
        return Results.Ok(records);
    }

    private static async Task<IResult> CreateStudentAssignment(
        CreateStudentAssignmentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var studentAssignment = await db.StudentAssignments
            .FirstOrDefaultAsync(sa => sa.StudentId == command.StudentId && sa.AssignmentId == command.AssignmentId, cancellationToken);

        if (studentAssignment != null)
        {
            return Results.BadRequest("Student assignment already exists.");
        }

        var student = await db.Students.FindAsync([command.StudentId], cancellationToken);
        if (student == null)
        {
            return Results.NotFound("Student not found.");
        }

        var assignment = await db.Assignments.FindAsync([command.AssignmentId], cancellationToken);
        if (assignment == null)
        {
            return Results.NotFound("Assignment not found.");
        }

        studentAssignment = new StudentAssignment
        {
            StudentId = command.StudentId,
            AssignmentId = command.AssignmentId,
            TempMark = command.TempMark,
            FinalMark = command.FinalMark
        };

        db.StudentAssignments.Add(studentAssignment);
        await db.SaveChangesAsync(cancellationToken);

        var record = new StudentAssignmentRecord(
            studentAssignment.StudentId, studentAssignment.AssignmentId, studentAssignment.TempMark, studentAssignment.FinalMark);
        return Results.Created($"/internal/student-assignments/{studentAssignment.StudentId}/{studentAssignment.AssignmentId}", record);
    }

    private static async Task<IResult> UpdateStudentAssignment(
        UpdateStudentAssignmentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var studentAssignment = await db.StudentAssignments
            .FirstOrDefaultAsync(sa => sa.StudentId == command.StudentId && sa.AssignmentId == command.AssignmentId, cancellationToken);

        if (studentAssignment == null)
        {
            return Results.NotFound();
        }

        studentAssignment.TempMark = command.TempMark;
        studentAssignment.FinalMark = command.FinalMark;

        await db.SaveChangesAsync(cancellationToken);

        var record = new StudentAssignmentRecord(
            studentAssignment.StudentId, studentAssignment.AssignmentId, studentAssignment.TempMark, studentAssignment.FinalMark);
        return Results.Ok(record);
    }

    private static async Task<IResult> DeleteStudentAssignment(
        [FromRoute] Guid studentId,
        [FromRoute] Guid assignmentId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (studentId == Guid.Empty || assignmentId == Guid.Empty)
        {
            return Results.BadRequest("Student ID and Assignment ID cannot be empty.");
        }

        var studentAssignment = await db.StudentAssignments
            .FirstOrDefaultAsync(sa => sa.StudentId == studentId && sa.AssignmentId == assignmentId, cancellationToken);

        if (studentAssignment == null)
        {
            return Results.NotFound();
        }

        db.StudentAssignments.Remove(studentAssignment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}