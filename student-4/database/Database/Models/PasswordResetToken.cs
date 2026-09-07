namespace Database.Models;

// A one-time-use password-reset token. The `TokenHash` column stores
// SHA-256(token) — we never persist the raw token. The raw token is
// what we put in the email; on submit, the API hashes the supplied
// value with the same algorithm and looks it up.
//
// The token is invalidated by either:
//   * being used (UsedAtUtc != null), or
//   * passing its ExpiresAtUtc.
public class PasswordResetToken
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
