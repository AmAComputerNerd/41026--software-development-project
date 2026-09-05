using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dataSource = db.Database.GetDbConnection().DataSource;
        var databaseDirectory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        await db.Database.MigrateAsync();
        await DbSeeder.SeedDataAsync(db);
    }
}