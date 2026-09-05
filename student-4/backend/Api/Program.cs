using System.Text.Json.Serialization;
using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
// Serialize enums as their string names (e.g. "Student") instead of
// integer values, so the frontend doesn't have to map numeric codes
// to friendly names for every UserType / Gender / CourseStatus /
// EmploymentStatus field.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options
        .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSeeding((db, _) => DbSeeder.SeedData((AppDbContext)db))
        .UseAsyncSeeding((db, _, _) =>
        {
            DbSeeder.SeedData((AppDbContext)db);
            return Task.CompletedTask;
        });
});
builder.Services.AddHttpClient();
// builder.Services.AddScoped<IAiDigestService, OpenRouterDigestService>();
builder.Services.AddScoped<IAiProfileSummaryService, OpenRouterProfileSummaryService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5199"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(builder.Configuration["AiGateway:BaseUrl"]))
{
    Log.AiGatewayBaseUrlNotSet(app.Logger);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapStudentEndpoints();
app.MapTeacherEndpoints();
app.MapUserCourseEndpoints();
app.MapProfileSummaryEndpoints();

// Infrastructure
app.UseApiExceptionHandling();
app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

// Run the seeder on every startup (not just during EF seeding) so
// fix-ups and missing detail records are always applied.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbSeeder.SeedData(db);
}

app.Run();

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning, Message =
        "AiGateway:BaseUrl is not set. AI generation will fail until you set it " +
        "(see student-4/backend/README.md).")]
    public static partial void AiGatewayBaseUrlNotSet(ILogger logger);
}