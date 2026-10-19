using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MultiAgentServer.Configuration;
using MultiAgentServer.Endpoints;
using MultiAgentServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<MultiAgentOptions>()
    .Bind(builder.Configuration.GetSection(MultiAgentOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.AiGatewayBaseUrl, UriKind.Absolute, out _),
        "MultiAgent:AiGatewayBaseUrl must be a valid absolute URL.")
    .ValidateOnStart();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<WorkflowStore>();
builder.Services.AddScoped<MultiAgentCoordinator>();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live", "ready"]);

var app = builder.Build();

app.MapGet("/", (WorkflowStore store) => Results.Ok(new
{
    service = "multi-agent-server",
    status = "ready",
    agents = Agents,
    workflowCount = store.List().Count
}));

app.MapWorkflowEndpoints();
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

public partial class Program
{
    private static readonly string[] Agents = ["Planner", "Worker", "Reviewer", "HumanReview"];
}
