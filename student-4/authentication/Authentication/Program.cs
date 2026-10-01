using System.Text.Json.Serialization;
using Authentication.Configuration;
using Authentication.Endpoints;
using Authentication.Extensions;
using Authentication.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using DotNetEnv;

var envPath = FindEnvFilePath();
if (!string.IsNullOrEmpty(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine("No .env file found in current or parent directories.");
}

// Map SMTP_* env vars to Email__Smtp__* expected by EmailOptions
MapSmtpEnvVars();

var builder = WebApplication.CreateBuilder(args);

#region Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddOptions<DatabaseServiceOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "DatabaseService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<NotificationServiceOptions>()
    .Bind(builder.Configuration.GetSection(NotificationServiceOptions.SectionName))
    .Validate(
        options => IsAbsoluteHttpUrl(options.BaseUrl),
        "NotificationService:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();

builder.Services
    .AddHttpClient<INotificationClient, NotificationClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<NotificationServiceOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });

builder.Services
    .AddHttpClient<IAccountDatabaseClient, AccountDatabaseClient>((services, client) =>
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<DatabaseServiceOptions>>().Value.BaseUrl))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(250);
        options.Retry.DisableForUnsafeHttpMethods();
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
    });
builder.Services
    .AddHttpClient(RemoteServiceHealthCheck.DatabaseServiceClientName, (services, client) =>
    {
        ConfigureClient(
            client,
            services.GetRequiredService<IOptions<DatabaseServiceOptions>>().Value.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(3);
    });

builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddSingleton<PasswordResetTokenGenerator>();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseServiceHealthCheck>("database-service", tags: ["ready"]);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5199"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});
#endregion

var app = builder.Build();

// Log email configuration at startup
using (var scope = app.Services.CreateScope())
{
    var emailOptions = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value;
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Email configuration loaded: Host={Host}, Port={Port}, UseSsl={UseSsl}, Username={Username}, FromAddress={FromAddress}, FromName={FromName}",
        emailOptions.Smtp.Host,
        emailOptions.Smtp.Port,
        emailOptions.Smtp.UseSsl,
        emailOptions.Smtp.Username ?? "(none)",
        emailOptions.FromAddress,
        emailOptions.FromName);
}

#region Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapAuthEndpoints();

app.UseApiExceptionHandling();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live")
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready")
    });
#endregion

app.Run();

static bool IsAbsoluteHttpUrl(string value)
{
    return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

static void ConfigureClient(HttpClient client, string baseUrl)
{
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/", UriKind.Absolute);
    client.Timeout = Timeout.InfiniteTimeSpan;
}

static void MapSmtpEnvVars()
{
    var mappings = new Dictionary<string, string>
    {
        ["SMTP_HOST"] = "Email__Smtp__Host",
        ["SMTP_PORT"] = "Email__Smtp__Port",
        ["SMTP_USERNAME"] = "Email__Smtp__Username",
        ["SMTP_PASSWORD"] = "Email__Smtp__Password",
        ["SMTP_USE_SSL"] = "Email__Smtp__UseSsl",
        ["SMTP_FROM_ADDRESS"] = "Email__FromAddress",
        ["SMTP_FROM_NAME"] = "Email__FromName"
    };

    foreach (var (src, dest) in mappings)
    {
        var value = Environment.GetEnvironmentVariable(src);
        if (!string.IsNullOrEmpty(value))
        {
            Environment.SetEnvironmentVariable(dest, value);
        }
    }
}

static string? FindEnvFilePath()
{
    var directories = new List<DirectoryInfo>
    {
        new(AppContext.BaseDirectory),
        new(Directory.GetCurrentDirectory())
    };

    foreach (var directory in directories)
    {
        var current = directory;
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }
    }

    return null;
}
