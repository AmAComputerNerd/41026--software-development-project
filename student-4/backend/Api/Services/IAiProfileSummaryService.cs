using Api.Models;

namespace Api.Services;

public interface IAiProfileSummaryService
{
    // Generates a short natural-language profile summary for the given
    // user, based on their base profile fields and any role-specific
    // (Student/Teacher) data that's present. Returns the summary text.
    Task<string> GenerateSummaryAsync(
        User user,
        Student? student,
        Teacher? teacher,
        CancellationToken cancellationToken = default);
}
