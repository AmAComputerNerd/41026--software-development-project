using GradesManager.Data;
using GradesManager.Endpoints;
using GradesManager.Extensions;
using GradesManager.Configuration;
using Microsoft.EntityFrameworkCore;
using GradesManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.Configure<AiGatewayOptions>(
    builder.Configuration.GetSection("AiGatewayOptions.SectionName"));
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
builder.Services.AddHttpClient<IAiTaskService, AiTaskService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

//map endpoints
app.MapCourseEndpoints();
app.MapStudentEndpoints();
app.MapAssignmentEndpoints();

app.UseHttpsRedirection();
await app.InitialiseDatabaseAsync();

app.Run();
