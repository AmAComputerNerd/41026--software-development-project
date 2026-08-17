using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DatabaseExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        
        await db.Database.MigrateAsync();
        DbSeeder.SeedData(db);
    }
}