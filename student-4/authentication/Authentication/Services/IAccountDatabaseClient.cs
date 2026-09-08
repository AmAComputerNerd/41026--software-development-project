using Student4.Contracts;

namespace Authentication.Services;

public interface IAccountDatabaseClient
{
    #region User lookup
    Task<IReadOnlyList<UserRecord>> GetUsersAsync(CancellationToken cancellationToken);
    #endregion

    #region Auth
    Task<UserRecord?> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<UserRecord?> LoginWithStatusAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<bool> ChangePasswordAsync(ChangePasswordCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteAccountAsync(DeleteAccountCommand command, CancellationToken cancellationToken);
    #endregion

    #region Password reset
    Task<PasswordResetTokenRecord?> CreatePasswordResetTokenAsync(
        CreatePasswordResetTokenCommand command,
        CancellationToken cancellationToken);

    Task<UserRecord?> ResetPasswordWithTokenAsync(
        ResetPasswordWithTokenCommand command,
        CancellationToken cancellationToken);
    #endregion
}
