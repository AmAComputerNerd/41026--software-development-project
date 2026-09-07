using Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Database.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<UserCourse> UserCourses { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<Student>()
            .HasKey(s => s.UserId);

        modelBuilder.Entity<Student>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId);

        modelBuilder.Entity<Teacher>()
            .HasKey(n => n.UserId);

        modelBuilder.Entity<Teacher>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<Teacher>(t => t.UserId);

        modelBuilder.Entity<UserCourse>()
            .HasKey(uc => new { uc.UserId, uc.CourseId });

        // PasswordResetToken: the API looks rows up by TokenHash, so
        // we need a unique index on it. Cascade-delete the rows when
        // their owning user is removed so we don't accumulate
        // orphans (e.g. when an account is deleted).
        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
