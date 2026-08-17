using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<Course> Courses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>()
            .HasKey(c => c.Id);
        
        modelBuilder.Entity<TaskEntity>()
            .HasKey(t => t.Id);
        
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
    }
}