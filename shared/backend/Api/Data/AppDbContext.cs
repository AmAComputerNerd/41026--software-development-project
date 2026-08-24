using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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
    }
}
