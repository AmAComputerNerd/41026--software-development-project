using GradesManager.Data;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Extensions
{
    public static class DatabaseExtensions
    {
        private static readonly TimeSpan MigrationTimeout = TimeSpan.FromMinutes(3);
        private static readonly int MaxMigrationRetries = 3;
        private static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(5);

        public static async Task InitialiseDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Exception? lastException = null;

            // EF Core SQLite migrations acquire an exclusive lock (__EFMigrationsLock).
            // Retry with a per-attempt timeout so a transient lock contention or a
            // slow CI runner does not leave the database service hanging forever
            // before it can start serving health checks.
            for (int attempt = 1; attempt <= MaxMigrationRetries; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(MigrationTimeout);
                    await db.Database.MigrateAsync(cts.Token);
                    lastException = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt < MaxMigrationRetries)
                    {
                        await Task.Delay(MigrationRetryDelay);
                    }
                }
            }

            if (lastException is not null)
            {
                throw lastException;
            }

            DbSeeder.SeedData(db);
        }
    }
}
