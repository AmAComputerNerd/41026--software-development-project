using McpServer.Configuration;
using McpServer.Services;
using McpServer.Tools;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<Student3Options>()
    .Bind(builder.Configuration.GetSection(Student3Options.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Student3:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddHttpClient<IDeadlineClient, DeadlineClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<Student3Options>>().Value;
        client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services
    .AddOptions<Student5Options>()
    .Bind(builder.Configuration.GetSection(Student5Options.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Student5:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddHttpClient<IGradesClient, GradesClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<Student5Options>>().Value;
        client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services
    .AddOptions<Student1Options>()
    .Bind(builder.Configuration.GetSection(Student1Options.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Student1:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddHttpClient<INotificationClient, NotificationClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<Student1Options>>().Value;
        client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services
    .AddOptions<Student4Options>()
    .Bind(builder.Configuration.GetSection(Student4Options.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Student4:BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();
builder.Services
    .AddHttpClient<IAccountClient, AccountClient>((services, client) =>
    {
        var options = services.GetRequiredService<IOptions<Student4Options>>().Value;
        client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<DeadlineTools>()
    .WithTools<NotificationTools>()
    .WithTools<GradesTools>();
    .WithTools<AccountTools>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live", "ready"]);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "mcp-server",
    status = "ready"
}));
app.MapMcp("/mcp");
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

app.Run();
