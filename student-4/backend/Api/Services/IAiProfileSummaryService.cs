using Student4.Contracts;

namespace Api.Services;

public interface IAiProfileSummaryService
{
    // Generates a short natural-language profile summary for the given
    // user, based on their base profile fields and any role-specific
    // (Student/Teacher) data that's present. Returns the summary text.
    Task<string> GenerateSummaryAsync(
        UserRecord user,
        StudentRecord? student,
        TeacherRecord? teacher,
        CancellationToken cancellationToken = default);
}
