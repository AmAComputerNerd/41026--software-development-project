using GradesManager.Data;
using GradesManager.Endpoints;
using GradesManager.Extensions;
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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map internal endpoints
app.MapCourseEndpoints();
app.MapStudentEndpoints();
app.MapAssignmentEndpoints();
app.MapStudentAssignmentEndpoints();
app.MapStudentCourseEndpoints();

// Health checks
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();