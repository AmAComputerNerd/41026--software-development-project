using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    private static readonly Guid Course1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Canonical seed-data emails. The seeder is idempotent: it always
    // ensures these accounts exist with the correct type, but it won't
    // blow away other data the user has created through the API.

    public static void SeedData(AppDbContext db)
    {
        SeedUsers(db);
        SeedStudents(db);
        SeedTeachers(db);
        SeedUserCourse(db);
    }

    private static void SeedUsers(AppDbContext db)
    {
        // Student seed
        var existingStudent = db.Users.FirstOrDefault(u => u.Email == "test1@student.uts.edu.au");
        if (existingStudent is null)
        {
            db.Users.Add(new User
            {
                Email = "test1@student.uts.edu.au",
                PasswordHash = "abc123",
                FirstName = "John",
                LastName = "Student",
                Gender = Gender.NonBinary,
                DateOfBirth = new DateTime(2008, 5, 1),
                UserType = UserType.Student,
            });
        }
        else if (existingStudent.UserType != UserType.Student)
        {
            // Fix-up: an account with the canonical student email was
            // created with the wrong type. Now that UserType has a
            // setter, we can just update it in place.
            existingStudent.UserType = UserType.Student;
        }

        // Teacher seed
        var existingTeacher = db.Users.FirstOrDefault(u => u.Email == "test1@faculty.uts.edu.au");
        if (existingTeacher is null)
        {
            db.Users.Add(new User
            {
                Email = "test1@faculty.uts.edu.au",
                PasswordHash = "123abc",
                FirstName = "Jane",
                LastName = "Teacher",
                Gender = Gender.Female,
                DateOfBirth = new DateTime(1988, 5, 1),
                UserType = UserType.Teacher,
            });
        }
        else if (existingTeacher.UserType != UserType.Teacher)
        {
            // Fix-up: an account with the canonical teacher email was
            // created with the wrong type. Now that UserType has a
            // setter, we can just update it in place.
            existingTeacher.UserType = UserType.Teacher;
        }

        db.SaveChanges();
    }

    private static void SeedStudents(AppDbContext db)
    {
        var student = db.Users.FirstOrDefault(u => u.Email == "test1@student.uts.edu.au");
        if (student is null) return;

        if (!db.Students.Any(s => s.UserId == student.Id))
        {
            db.Students.Add(new Student(student.Id)
            {
                CourseStatus = CourseStatus.FullTime,
                IsInternational = true,
                CanvasApiKey = "implement later",
            });
            db.SaveChanges();
        }
    }

    private static void SeedTeachers(AppDbContext db)
    {
        var teacher = db.Users.FirstOrDefault(u => u.Email == "test1@faculty.uts.edu.au");
        if (teacher is null) return;

        if (!db.Teachers.Any(t => t.UserId == teacher.Id))
        {
            db.Teachers.Add(new Teacher(teacher.Id)
            {
                EmploymentStatus = EmploymentStatus.FullTime,
                CanvasApiKey = "implement later",
            });
            db.SaveChanges();
        }
    }

    private static void SeedUserCourse(AppDbContext db)
    {
        var student = db.Users.FirstOrDefault(u => u.Email == "test1@student.uts.edu.au");
        var teacher = db.Users.FirstOrDefault(u => u.Email == "test1@faculty.uts.edu.au");
        if (student is null || teacher is null) return;

        if (db.UserCourses.Find(student.Id, Course1Id) is null)
        {
            db.UserCourses.Add(new UserCourse(student.Id, Course1Id));
        }
        if (db.UserCourses.Find(teacher.Id, Course1Id) is null)
        {
            db.UserCourses.Add(new UserCourse(teacher.Id, Course1Id));
        }
        db.SaveChanges();
    }
}
