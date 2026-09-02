using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter =
        new(
            value => value.HasValue ? ToUtc(value.Value) : value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value);

    public DbSet<TaskEntity> Tasks { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Course>()
            .HasIndex(c => c.CanvasCourseId)
            .IsUnique();

        modelBuilder.Entity<TaskEntity>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<TaskEntity>()
            .HasIndex(t => t.CanvasAssignmentId)
            .IsUnique();

        modelBuilder.Entity<TaskEntity>()
            .HasOne(t => t.Course)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TaskEntity>()
            .HasOne(t => t.ParentTask)
            .WithMany(t => t.ChildrenTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskEntity>()
            .Property(t => t.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<TaskEntity>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Course>()
            .Property(course => course.LastCanvasSyncAt)
            .HasConversion(UtcNullableDateTimeConverter);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.DueDate)
            .HasConversion(UtcNullableDateTimeConverter);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.CreatedAt)
            .HasConversion(UtcDateTimeConverter);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.UpdatedAt)
            .HasConversion(UtcDateTimeConverter);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.CanvasUpdatedAt)
            .HasConversion(UtcNullableDateTimeConverter);

        modelBuilder.Entity<TaskEntity>()
            .Property(task => task.DueSoonReminderSentAtUtc)
            .HasConversion(UtcNullableDateTimeConverter);
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