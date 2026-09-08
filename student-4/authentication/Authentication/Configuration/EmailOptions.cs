namespace Authentication.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public SmtpOptions Smtp { get; init; } = new();
    public PasswordResetOptions PasswordReset { get; init; } = new();

    public string FromAddress { get; init; } = "no-reply@example.com";
    public string FromName { get; init; } = "Account Service";
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1025;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
}

public sealed class PasswordResetOptions
{
    public string BaseUrl { get; init; } = "http://localhost:8080/account/reset-password";

    public int TokenLifetimeMinutes { get; init; } = 60;
}
