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
        public DbSet<StudentAssignment> StudentAssignments { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //primary keys
            modelBuilder.Entity<StudentCourse>()
                .HasKey(sc => new { sc.StudentId, sc.CourseId });
            modelBuilder.Entity<Student>()
                .HasKey(s => s.StudentId);
            modelBuilder.Entity<Course>()
                .HasKey(c => c.CourseId);
            modelBuilder.Entity<Assignment>()
                .HasKey(a => a.AssignmentId);
            modelBuilder.Entity<StudentAssignment>()
                .HasKey(sa => new { sa.StudentId, sa.AssignmentId });

            //foreign keys
            modelBuilder.Entity<StudentCourse>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId);
            modelBuilder.Entity<StudentCourse>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId);

            modelBuilder.Entity<Assignment>()
                .HasOne(c => c.Course)
                .WithMany()
                .HasForeignKey(c => c.CourseId);

            modelBuilder.Entity<StudentAssignment>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId);
            modelBuilder.Entity<StudentAssignment>()
                .HasOne(c => c.Assignment)
                .WithMany()
                .HasForeignKey(c => c.AssignmentId);
        }
    }
}
