using Student4.Contracts;

namespace Api.Services;

// HTTP client for the student-4-database service, scoped to the
// profile-CRUD surface (users, students, teachers, profile summary).
// The auth surface (login, change-password, delete-account, password
// reset) lives in the standalone Authentication service, which
// has its own scoped IAccountDatabaseClient implementation.
public interface IAccountDatabaseClient
{
    // ---- Users ----
    Task<IReadOnlyList<UserRecord>> GetUsersAsync(CancellationToken cancellationToken);
    Task<UserRecord?> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<UserRecord> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken);
    Task<UserRecord?> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteUserAsync(Guid id, CancellationToken cancellationToken);

    // ---- Students ----
    Task<StudentRecord?> GetStudentAsync(Guid userId, CancellationToken cancellationToken);
    Task<StudentRecord> UpdateStudentAsync(Guid userId, UpdateStudentCommand command, CancellationToken cancellationToken);

    // ---- Teachers ----
    Task<TeacherRecord?> GetTeacherAsync(Guid userId, CancellationToken cancellationToken);
    Task<TeacherRecord> UpdateTeacherAsync(Guid userId, UpdateTeacherCommand command, CancellationToken cancellationToken);

    // ---- Profile summary ----
    Task<UserRecord?> UpdateProfileSummaryAsync(Guid userId, ProfileSummaryCommand command, CancellationToken cancellationToken);
}
