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

// Returned from CreatePasswordResetToken and GetPasswordResetToken
// in the database service. The token hash is intentionally not part
// of this record — the database service never re-exposes hashes.
public sealed record PasswordResetTokenRecord(
    long Id,
    Guid UserId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? UsedAtUtc);

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

// ---- Password reset ----

// Persists a freshly-issued reset token. The `TokenHash` is SHA-256
// of the raw token (which is what we put in the email); the raw
// token is never stored. `ExpiresAtUtc` is an absolute UTC time so
// the database service doesn't have to know about token-lifetime
// configuration.
public sealed record CreatePasswordResetTokenCommand(
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAtUtc);

// Looks up a token by its hash. Returns the record if the token
// exists, is unused, and has not expired; otherwise null. The
// database service is the right place to enforce these constraints
// in a single round-trip — the API shouldn't have to know.
public sealed record RedeemPasswordResetTokenCommand(
    string TokenHash);

// Result of a successful redemption — the API hashes the new
// password (it already has the plain text from the request) and the
// database service does the actual update against `Users`.
public sealed record ResetPasswordWithTokenCommand(
    string TokenHash,
    string NewPasswordHash);
