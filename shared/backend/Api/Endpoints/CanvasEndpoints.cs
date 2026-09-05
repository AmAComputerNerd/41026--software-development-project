using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class CanvasEndpoints
{
    public static IEndpointRouteBuilder MapCanvasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/canvas");
        group.MapGet("/courses", GetCourses);
        group.MapGet("/courses/{courseId:long}/assignments", GetAssignments);
        group.MapGet("/courses/{courseId:long}/users", GetUsers);
        group.MapGet("/courses/{courseId:long}/recipients", GetRecipients);
        group.MapPost("/conversations", CreateConversation);
        group.MapGet("/courses/{courseId:long}/quizzes", GetQuizzes);
        group.MapPost(
            "/courses/{courseId:long}/quizzes/{quizId:long}/submissions",
            StartQuizSubmission);
        group.MapGet(
            "/quiz-submissions/{quizSubmissionId:long}/questions",
            GetQuizSubmissionQuestions);
        group.MapPost(
            "/quiz-submissions/{quizSubmissionId:long}/answers",
            AnswerQuizSubmissionQuestions);
        return endpoints;
    }

    private static async Task<IResult> GetCourses(
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var courses = await canvas.GetCoursesAsync(cancellationToken);
        return Results.Ok(courses);
    }

    private static async Task<IResult> GetAssignments(
        [FromRoute] long courseId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var assignments = await canvas.GetAssignmentsAsync(courseId, cancellationToken);
        return Results.Ok(assignments);
    }

    private static async Task<IResult> GetUsers(
        [FromRoute] long courseId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var users = await canvas.GetUsersForCourseAsync(courseId, cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> GetRecipients(
        [FromRoute] long courseId,
        [FromQuery] string? search,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var recipients = await canvas.FindRecipientsAsync(courseId, search, cancellationToken);
        return Results.Ok(recipients);
    }

    private static async Task<IResult> CreateConversation(
        CreateCanvasConversationDto request,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        if (request.Recipients.Count == 0 || request.Recipients.Any(string.IsNullOrWhiteSpace))
        {
            return Results.BadRequest("At least one recipient is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest("`body` is required.");
        }

        if (request.Subject.Length > 255)
        {
            return Results.BadRequest("`subject` cannot exceed 255 characters.");
        }

        if (!request.ContextCode.StartsWith("course_", StringComparison.Ordinal) &&
            !request.ContextCode.StartsWith("group_", StringComparison.Ordinal))
        {
            return Results.BadRequest("`contextCode` must identify a Canvas course or group.");
        }

        var conversations = await canvas.CreateConversationAsync(request, cancellationToken);
        return Results.Ok(conversations);
    }

    private static async Task<IResult> GetQuizzes(
        [FromRoute] long courseId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var quizzes = await canvas.GetQuizzesAsync(courseId, cancellationToken);
        return Results.Ok(quizzes);
    }

    private static async Task<IResult> StartQuizSubmission(
        [FromRoute] long courseId,
        [FromRoute] long quizId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var submission = await canvas.StartQuizSubmissionAsync(courseId, quizId, cancellationToken);
        return Results.Ok(submission);
    }

    private static async Task<IResult> GetQuizSubmissionQuestions(
        [FromRoute] long quizSubmissionId,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        var questions = await canvas.GetQuizSubmissionQuestionsAsync(
            quizSubmissionId,
            cancellationToken);
        return Results.Ok(questions);
    }

    private static async Task<IResult> AnswerQuizSubmissionQuestions(
        [FromRoute] long quizSubmissionId,
        AnswerCanvasQuizQuestionsDto request,
        CanvasFacade canvas,
        CancellationToken cancellationToken)
    {
        if (request.Answers.Count == 0)
        {
            return Results.BadRequest("At least one answer is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ValidationToken))
        {
            return Results.BadRequest("`validationToken` is required.");
        }

        await canvas.AnswerQuizSubmissionQuestionsAsync(
            quizSubmissionId,
            request,
            cancellationToken);
        return Results.NoContent();
    }
}
