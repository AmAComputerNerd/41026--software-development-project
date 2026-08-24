using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public DbSet<CanvasRequestLog> CanvasRequestLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CanvasRequestLog>()
            .HasKey(log => log.Id);

        modelBuilder.Entity<CanvasRequestLog>()
            .Property(log => log.Operation)
            .HasMaxLength(200);

        modelBuilder.Entity<CanvasRequestLog>()
            .HasIndex(log => log.StartedAt);

        modelBuilder.Entity<CanvasRequestLog>()
            .Property(log => log.StartedAt)
            .HasConversion(UtcDateTimeConverter);

        modelBuilder.Entity<CanvasRequestLog>()
            .Property(log => log.CompletedAt)
            .HasConversion(UtcDateTimeConverter);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
