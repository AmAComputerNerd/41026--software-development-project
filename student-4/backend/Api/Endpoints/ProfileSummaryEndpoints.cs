using Api.Services;
using Student4.Contracts;

namespace Api.Endpoints;

public static class ProfileSummaryEndpoints
{
    public static IEndpointRouteBuilder MapProfileSummaryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users");

        group.MapPost("/{userId:guid}/profile-summary", GenerateProfileSummary);

        return endpoints;
    }

    private static async Task<IResult> GenerateProfileSummary(
        Guid userId,
        IAccountDatabaseClient db,
        IAiProfileSummaryService aiService,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await db.GetUserAsync(userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            StudentRecord? student = null;
            TeacherRecord? teacher = null;
            if (user.UserType == "Student")
            {
                student = await db.GetStudentAsync(userId, cancellationToken);
            }
            else if (user.UserType == "Teacher")
            {
                teacher = await db.GetTeacherAsync(userId, cancellationToken);
            }

            var summary = await aiService.GenerateSummaryAsync(
                user,
                student,
                teacher,
                cancellationToken);

            var updated = await db.UpdateProfileSummaryAsync(
                userId,
                new ProfileSummaryCommand(summary),
                cancellationToken);

            return Results.Ok(new { summary = updated?.UserProfile ?? summary });
        }
        catch (DatabaseServiceException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
    }
}
