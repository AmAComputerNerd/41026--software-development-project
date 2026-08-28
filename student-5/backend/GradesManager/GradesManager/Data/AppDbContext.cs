using GradesManager.Models;
using Microsoft.EntityFrameworkCore;

namespace GradesManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<StudentCourse> StudentCourses { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //primary keys
            modelBuilder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentID, sc.CourseID });
            modelBuilder.Entity<Student>()
                .HasKey(s => s.StudentID);
            modelBuilder.Entity<Course>()
                .HasKey(c => c.CourseId);
            modelBuilder.Entity<Assignment>()
                .HasKey(a => a.AssignmentID);

            //foreign keys
            modelBuilder.Entity<StudentCourse>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentID);
            modelBuilder.Entity<StudentCourse>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseID);

            modelBuilder.Entity<Assignment>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseID);
        }
    }
}
