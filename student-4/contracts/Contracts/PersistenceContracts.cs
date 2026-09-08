namespace Student4.Contracts;

#region Read records
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

public sealed record PasswordResetTokenRecord(
    long Id,
    Guid UserId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? UsedAtUtc);
#endregion

#region Commands
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
#endregion

#region Password reset
public sealed record CreatePasswordResetTokenCommand(
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAtUtc);

public sealed record RedeemPasswordResetTokenCommand(
    string TokenHash);

public sealed record ResetPasswordWithTokenCommand(
    string TokenHash,
    string NewPasswordHash);
#endregion
