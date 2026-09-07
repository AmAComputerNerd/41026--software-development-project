namespace Database.Services;

// BCrypt-based password hasher. The hash string contains its own salt
// and work-factor prefix, so it is safe to store as-is. The plain-text
// password is never persisted by the database service — every place
// that accepts a password from the API hashes it before it touches
// the DbContext, and every place that checks a password uses Verify.
public static class PasswordHasher
{
    // Work factor 11 is the BCrypt.Net-Next default and a reasonable
    // trade-off between login latency (~150ms on a modern CPU) and
    // resistance to offline brute-force.
    private const int WorkFactor = 11;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be empty.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // The stored value is not a valid BCrypt hash (e.g. a
            // legacy plain-text password from the pre-hashing era).
            // Treat as "no match" so the caller can reject the login
            // and the user can be prompted to reset their password.
            return false;
        }
    }
}
