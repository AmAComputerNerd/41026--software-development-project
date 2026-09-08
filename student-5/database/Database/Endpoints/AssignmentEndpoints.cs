using GradesManager.Contracts;
using GradesManager.Data;
using GradesManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Endpoints;

public static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/internal/assignments");

        group.MapGet("/", GetAssignments);
        group.MapGet("/{id:guid}", GetAssignment);
        group.MapGet("/student/{studentId:guid}", GetAssignmentsByStudent);
        group.MapGet("/course/{courseId:guid}", GetAssignmentsByCourse);
        group.MapPost("/", CreateAssignment);
        group.MapPut("/{id:guid}", UpdateAssignment);
        group.MapDelete("/{id:guid}", DeleteAssignment);

        return endpoints;
    }

    private static async Task<IResult> GetAssignments(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var assignments = await db.Assignments.AsNoTracking().ToListAsync(cancellationToken);
        var records = assignments.Select(a => new AssignmentRecord(
            a.AssignmentId, a.CourseId, a.Name, a.Weight, a.MaxMark, a.Completed));
        return Results.Ok(records);
    }

    private static async Task<IResult> GetAssignment(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        return assignment == null
            ? Results.NotFound()
            : Results.Ok(new AssignmentRecord(
                assignment.AssignmentId, assignment.CourseId, assignment.Name, assignment.Weight, assignment.MaxMark, assignment.Completed));
    }

    private static async Task<IResult> GetAssignmentsByStudent(
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

        var assignments = await db.StudentAssignments
            .Where(sa => sa.StudentId == studentId)
            .Select(sa => sa.Assignment)
            .ToListAsync(cancellationToken);

        var records = assignments
            .Where(a => a != null)
            .Select(a => new AssignmentRecord(
                a!.AssignmentId, a.CourseId, a.Name, a.Weight, a.MaxMark, a.Completed));
        return Results.Ok(records);
    }

    private static async Task<IResult> GetAssignmentsByCourse(
        [FromRoute] Guid courseId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return Results.BadRequest("Course ID cannot be empty.");
        }

        var course = await db.Courses.FindAsync([courseId], cancellationToken);
        if (course == null)
        {
            return Results.NotFound();
        }

        var assignments = await db.Assignments
            .Where(a => a.CourseId == courseId)
            .ToListAsync(cancellationToken);

        var records = assignments.Select(a => new AssignmentRecord(
            a.AssignmentId, a.CourseId, a.Name, a.Weight, a.MaxMark, a.Completed));
        return Results.Ok(records);
    }

    private static async Task<IResult> CreateAssignment(
        CreateAssignmentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var assignment = new Assignment
        {
            CourseId = command.CourseId,
            Name = command.Name,
            Weight = command.Weight,
            MaxMark = command.MaxMark,
            Completed = command.Completed
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        var record = new AssignmentRecord(
            assignment.AssignmentId, assignment.CourseId, assignment.Name, assignment.Weight, assignment.MaxMark, assignment.Completed);
        return Results.Created($"/internal/assignments/{assignment.AssignmentId}", record);
    }

    private static async Task<IResult> UpdateAssignment(
        [FromRoute] Guid id,
        UpdateAssignmentCommand command,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (id != command.AssignmentId)
        {
            return Results.BadRequest("Assignment ID mismatch.");
        }

        var assignment = await db.Assignments.FindAsync([id], cancellationToken);
        if (assignment == null)
        {
            return Results.NotFound();
        }

        assignment.CourseId = command.CourseId;
        assignment.Name = command.Name;
        assignment.Weight = command.Weight;
        assignment.MaxMark = command.MaxMark;
        assignment.Completed = command.Completed;

        await db.SaveChangesAsync(cancellationToken);

        var record = new AssignmentRecord(
            assignment.AssignmentId, assignment.CourseId, assignment.Name, assignment.Weight, assignment.MaxMark, assignment.Completed);
        return Results.Ok(record);
    }

    private static async Task<IResult> DeleteAssignment(
        [FromRoute] Guid id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FindAsync([id], cancellationToken);
        if (assignment == null)
        {
            return Results.NotFound();
        }

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}