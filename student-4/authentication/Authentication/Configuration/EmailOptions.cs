namespace Authentication.Configuration;

// All email-related configuration is namespaced under "Email". The
// `Smtp` sub-section mirrors standard SMTP host/port/credentials,
// and the `PasswordReset` sub-section configures the reset-link
// template (base URL of the frontend + token lifetime).
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
    public bool UseSsl { get; init; } = false;
}

public sealed class PasswordResetOptions
{
    // Base URL of the frontend reset-password page. The service
    // appends `?token=<token>` when building the link in the email.
    public string BaseUrl { get; init; } = "http://localhost:8080/account/reset-password";

    // How long a reset token is valid for. Default 60 minutes.
    public int TokenLifetimeMinutes { get; init; } = 60;
}
