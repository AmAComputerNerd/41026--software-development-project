using Api.Models;
using TaskStatus = Api.Models.TaskStatus;

namespace Api.Data;

public class DbSeeder
{
    public static void SeedData(AppDbContext db)
    {
        var courses = SeedCourses(db);
        SeedTasks(db, courses);
    }

    private static ICollection<Course> SeedCourses(AppDbContext db)
    {
        var courses = db.Courses;
        if (!courses.Any())
        {
            courses.AddRange(
                new Course
                {
                    Code = "41026",
                    Name = "Advanced Software Development"
                },
                new Course
                {
                    Code = "37181",
                    Name = "Discrete Mathematics"
                },
                new Course
                {
                    Code = "43034",
                    Name = "Computing Science Learning Integrated Work C"
                },
                new Course
                {
                    Code = "31272",
                    Name = "Project Management and the Professional"
                },
                new Course
                {
                    Code = "31005",
                    Name = "Machine Learning"
                }
            );

            db.SaveChanges();
        }

        return courses.ToList();
    }

    private static ICollection<TaskEntity> SeedTasks(AppDbContext db, ICollection<Course> courses)
    {
        var tasks = db.Tasks;

        if (!tasks.Any())
        {
            tasks.AddRange(
                new TaskEntity
                {
                    Title = "Release 0",
                    Description = "Finish work on Release 0",
                    CourseId = courses.FirstOrDefault(c => c.Code == "41026")?.Id,
                    Priority = TaskPriority.High,
                    Status = TaskStatus.InProgress,
                    DueDate = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new TaskEntity
                {
                    Title = "Release 1",
                    Description = "Finish work on Release 1",
                    CourseId = courses.FirstOrDefault(c => c.Code == "41026")?.Id,
                    Priority = TaskPriority.Medium,
                    Status = TaskStatus.Todo,
                    DueDate = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new TaskEntity
                {
                    Title = "Release 2",
                    Description = "Finish work on Release 2",
                    CourseId = courses.FirstOrDefault(c => c.Code == "41026")?.Id,
                    Priority = TaskPriority.Low,
                    Status = TaskStatus.InProgress,
                    DueDate = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new TaskEntity
                {
                    Title = "Library Study Session",
                    Description = "Generic task without a course",
                    Priority = TaskPriority.Low,
                    Status = TaskStatus.Completed,
                    DueDate = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new TaskEntity
                {
                    Title = "Learning Machines",
                    Description = "AI :O",
                    CourseId = courses.FirstOrDefault(c => c.Code == "31005")?.Id,
                    Priority = TaskPriority.High,
                    Status = TaskStatus.Completed,
                    DueDate = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            );

            db.SaveChanges();
        }

        return tasks.ToList();
    }
}