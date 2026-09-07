namespace Student4.Contracts;

// These records are the only shape the public API and the database
// service share for persistence operations. They intentionally omit
// EF Core concerns (navigation properties, [JsonIgnore] hints, etc.)
// so neither service can accidentally reach into the other's data
// model.

// ---- Read records (returned by the database service to the API) ----

public sealed record UserRecord(
    Guid Id,
    string Email,
    string PasswordHash,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string UserType,
    string? UserProfile);

public sealed record StudentRecord(
    Guid UserId,
    string CourseStatus,
    bool IsInternational,
    string CanvasApiKey);

public sealed record TeacherRecord(
    Guid UserId,
    string EmploymentStatus,
    string CanvasApiKey);

// ---- Commands (sent from the API to the database service) ----

public sealed record CreateUserCommand(
    string Email,
    string PasswordHash,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string UserType,
    CreateStudentCommand? Student,
    CreateTeacherCommand? Teacher);

public sealed record CreateStudentCommand(
    string CourseStatus,
    bool IsInternational,
    string CanvasApiKey);

public sealed record CreateTeacherCommand(
    string EmploymentStatus,
    string CanvasApiKey);

public sealed record UpdateUserCommand(
    string Email,
    string FirstName,
    string? MiddleNames,
    string LastName,
    string Gender,
    DateTime DateOfBirth,
    string? UserProfile);

public sealed record UpdateStudentCommand(
    string? CourseStatus,
    bool? IsInternational,
    string? CanvasApiKey);

public sealed record UpdateTeacherCommand(
    string? EmploymentStatus,
    string? CanvasApiKey);

public sealed record ProfileSummaryCommand(string Summary);

public sealed record LoginCommand(string Email, string Password);

public sealed record ChangePasswordCommand(string Email, string CurrentPassword, string NewPassword);

public sealed record DeleteAccountCommand(string Email, string Password);
