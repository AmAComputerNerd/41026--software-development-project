using Student4.Contracts;

namespace Authentication.Services;

// HTTP client for the student-4-database service, scoped to the
// auth-related methods only. The full CRUD surface (users/students/
// teachers) lives in the main API project. This interface is what
// the auth service injects to talk to persistence.
public interface IAccountDatabaseClient
{
    // ---- User lookup (for password reset lookup-by-email) ----
    // The full CRUD surface lives in the main API; this slim variant
    // is just enough to find a user by email so we can issue a
    // password-reset token.
    Task<IReadOnlyList<UserRecord>> GetUsersAsync(CancellationToken cancellationToken);

    // ---- Auth ----
    Task<UserRecord?> LoginAsync(LoginCommand command, CancellationToken cancellationToken);

    // Like LoginAsync, but treats 401 Unauthorized (bad password) the
    // same as 404 Not Found - returning null instead of throwing. The
    // /api/auth/login endpoint needs to map a 401 from the database
    // service to a 401 from the public API, not 503.
    Task<UserRecord?> LoginWithStatusAsync(LoginCommand command, CancellationToken cancellationToken);

    Task<bool> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteAccountAsync(DeleteAccountCommand command, CancellationToken cancellationToken);

    // ---- Password reset ----
    // Issue a token for the given user. Returns the new token's Id and
    // expiry so the API can build the email body. The token hash
    // itself never leaves the database service.
    Task<PasswordResetTokenRecord?> CreatePasswordResetTokenAsync(
        CreatePasswordResetTokenCommand command,
        CancellationToken cancellationToken);

    // Burn a token and update the user's password in a single
    // database round-trip. Returns the updated user record (without
    // the password hash) on success, or null if the token is invalid
    // for any reason.
    Task<UserRecord?> ResetPasswordWithTokenAsync(
        ResetPasswordWithTokenCommand command,
        CancellationToken cancellationToken);
}
