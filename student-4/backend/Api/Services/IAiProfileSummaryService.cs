using Student4.Contracts;

namespace Api.Services;

public interface IAiProfileSummaryService
{
    Task<string> GenerateSummaryAsync(
        UserRecord user,
        StudentRecord? student,
        TeacherRecord? teacher,
        CancellationToken cancellationToken = default);
}
