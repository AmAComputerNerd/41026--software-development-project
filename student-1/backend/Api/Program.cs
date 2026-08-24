using Api.Data;
using Api.Endpoints;
using Api.Extensions;
using Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
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
builder.Services.AddScoped<IAiDigestService, OpenRouterDigestService>();
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
    app.Logger.LogWarning(
        "AiGateway:BaseUrl is not set. AI digest generation will fail until you set it " +
        "(see student-1/backend/README.md).");
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
app.MapNotificationEndpoints();
app.MapPreferenceEndpoints();
app.MapAiDigestEndpoints();

// Infrastructure
app.UseApiExceptionHandling();
app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();
