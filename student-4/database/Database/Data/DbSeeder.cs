using Database.Models;
using Database.Services;
using Microsoft.EntityFrameworkCore;

namespace Database.Data;

public static class DbSeeder
{
    private static readonly Guid Course1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // Plain-text seed passwords. The seeder hashes them on every run,
    // so a fresh database stores BCrypt hashes from the start. If the
    // seeder finds an existing user whose PasswordHash is not a BCrypt
    // hash (i.e. a legacy plain-text value from before this change),
    // it re-hashes the canonical password and overwrites the column.
    // That gives us a one-time migration of dev accounts without
    // requiring a separate data-fixup step.
    private const string StudentSeedPassword = "abc123";
    private const string TeacherSeedPassword = "123abc";

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

    private static bool IsHashedPassword(string value)
    {
        // BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$.
        return !string.IsNullOrEmpty(value) &&
            (value.StartsWith("$2a$", StringComparison.Ordinal) ||
             value.StartsWith("$2b$", StringComparison.Ordinal) ||
             value.StartsWith("$2x$", StringComparison.Ordinal) ||
             value.StartsWith("$2y$", StringComparison.Ordinal));
    }

    // ---- Bulk seed data ----

    private static readonly StudentSeed[] StudentSeeds =
    [
        new("test1",  "John",   "Student",  "NonBinary", new DateTime(2008, 5, 1),  CourseStatus.FullTime,  IsInternational: true),
        new("test2",  "Aisha",  "Nguyen",   "Female",   new DateTime(2006, 11, 12), CourseStatus.FullTime,  IsInternational: true),
        new("test3",  "Bilal",  "Hassan",   "Male",     new DateTime(2005, 3, 22),  CourseStatus.PartTime,  IsInternational: true),
        new("test4",  "Carlos", "Mendoza",  "Male",     new DateTime(2004, 8, 14),  CourseStatus.FullTime,  IsInternational: false),
        new("test5",  "Dani",   "Park",     "NonBinary", new DateTime(2007, 1, 30), CourseStatus.FullTime,  IsInternational: true),
        new("test6",  "Elena",  "Rossi",    "Female",   new DateTime(2006, 6, 9),   CourseStatus.PartTime,  IsInternational: true),
        new("test7",  "Finn",   "O'Connor", "Male",     new DateTime(2003, 12, 4),  CourseStatus.Inactive,  IsInternational: false),
        new("test8",  "Grace",  "Liu",      "Female",   new DateTime(2005, 9, 18),  CourseStatus.FullTime,  IsInternational: true),
        new("test9",  "Hugo",   "Mbeki",    "Male",     new DateTime(2004, 2, 25),  CourseStatus.FullTime,  IsInternational: false),
        new("test10", "Ines",   "Silva",    "Female",   new DateTime(2006, 10, 7),  CourseStatus.PartTime,  IsInternational: true),
        new("test11", "Jamal",  "Wright",   "Male",     new DateTime(2007, 4, 16),  CourseStatus.FullTime,  IsInternational: false),
    ];

    private static readonly TeacherSeed[] TeacherSeeds =
    [
        new("test1",  "Jane",   "Teacher",  "Female",   new DateTime(1988, 5, 1),  EmploymentStatus.FullTime),
        new("test2",  "Amir",   "Khan",     "Male",     new DateTime(1981, 7, 11),  EmploymentStatus.FullTime),
        new("test3",  "Beatrix","Hartmann", "Female",   new DateTime(1975, 2, 19),  EmploymentStatus.PartTime),
        new("test4",  "Chen",   "Wei",      "Male",     new DateTime(1990, 11, 3),  EmploymentStatus.FullTime),
        new("test5",  "Daniela","Costa",    "Female",   new DateTime(1985, 4, 27),  EmploymentStatus.FullTime),
        new("test6",  "Ethan",  "Brown",    "Male",     new DateTime(1979, 9, 8),   EmploymentStatus.PartTime),
        new("test7",  "Farah",  "Nadir",    "Female",   new DateTime(1983, 6, 14),  EmploymentStatus.Inactive),
        new("test8",  "Gareth", "Jones",    "Male",     new DateTime(1972, 1, 23),  EmploymentStatus.FullTime),
        new("test9",  "Hana",   "Yamada",   "Female",   new DateTime(1991, 12, 30), EmploymentStatus.FullTime),
        new("test10", "Ivan",   "Petrov",   "Male",     new DateTime(1986, 8, 5),   EmploymentStatus.PartTime),
        new("test11", "Jasmin", "Ali",      "Female",   new DateTime(1989, 3, 17),  EmploymentStatus.FullTime),
    ];

    // ---- Users ----

    private static void SeedUsers(AppDbContext db)
    {
        bool anyChanges = false;

        foreach (var seed in StudentSeeds)
        {
            if (UpsertUser(db, seed, isStudent: true))
            {
                anyChanges = true;
            }
        }

        foreach (var seed in TeacherSeeds)
        {
            if (UpsertUser(db, seed, isStudent: false))
            {
                anyChanges = true;
            }
        }

        if (anyChanges)
        {
            db.SaveChanges();
        }
    }

    private static bool UpsertUser(AppDbContext db, UserSeed seed, bool isStudent)
    {
        var email = seed.Email;
        var existing = db.Users.FirstOrDefault(u => u.Email == email);

        if (existing is null)
        {
            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(isStudent ? StudentSeedPassword : TeacherSeedPassword),
                FirstName = seed.FirstName,
                LastName = seed.LastName,
                Gender = Enum.Parse<Gender>(seed.Gender),
                DateOfBirth = seed.DateOfBirth,
                UserType = isStudent ? UserType.Student : UserType.Teacher,
            });
            return true;
        }

        bool changed = false;

        // Fix-up: an account with the canonical seed email was created
        // with the wrong type, or fields have drifted from the seed
        // definition. Idempotently refresh them.
        if (existing.UserType != (isStudent ? UserType.Student : UserType.Teacher))
        {
            existing.UserType = isStudent ? UserType.Student : UserType.Teacher;
            changed = true;
        }
        if (!string.Equals(existing.FirstName, seed.FirstName, StringComparison.Ordinal))
        {
            existing.FirstName = seed.FirstName;
            changed = true;
        }
        if (!string.Equals(existing.LastName, seed.LastName, StringComparison.Ordinal))
        {
            existing.LastName = seed.LastName;
            changed = true;
        }
        if (Enum.Parse<Gender>(seed.Gender) != existing.Gender)
        {
            existing.Gender = Enum.Parse<Gender>(seed.Gender);
            changed = true;
        }
        if (seed.DateOfBirth != existing.DateOfBirth)
        {
            existing.DateOfBirth = seed.DateOfBirth;
            changed = true;
        }

        // One-time migration: re-hash legacy plain-text passwords.
        if (!IsHashedPassword(existing.PasswordHash))
        {
            existing.PasswordHash = PasswordHasher.Hash(isStudent ? StudentSeedPassword : TeacherSeedPassword);
            changed = true;
        }

        return changed;
    }

    // ---- Role-specific tables ----

    private static void SeedStudents(AppDbContext db)
    {
        var pending = new List<Student>();
        foreach (var seed in StudentSeeds)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == seed.Email);
            if (user is null) continue;

            if (!db.Students.Any(s => s.UserId == user.Id))
            {
                pending.Add(new Student(user.Id)
                {
                    CourseStatus = seed.CourseStatus,
                    IsInternational = seed.IsInternational,
                    CanvasApiKey = "implement later",
                });
            }
        }

        if (pending.Count > 0)
        {
            db.Students.AddRange(pending);
            db.SaveChanges();
        }
    }

    private static void SeedTeachers(AppDbContext db)
    {
        var pending = new List<Teacher>();
        foreach (var seed in TeacherSeeds)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == seed.Email);
            if (user is null) continue;

            if (!db.Teachers.Any(t => t.UserId == user.Id))
            {
                pending.Add(new Teacher(user.Id)
                {
                    EmploymentStatus = seed.EmploymentStatus,
                    CanvasApiKey = "implement later",
                });
            }
        }

        if (pending.Count > 0)
        {
            db.Teachers.AddRange(pending);
            db.SaveChanges();
        }
    }

    // ---- Enrolments ----

    private static void SeedUserCourse(AppDbContext db)
    {
        var pending = new List<UserCourse>();
        foreach (var seed in StudentSeeds.Concat<UserSeed>(TeacherSeeds))
        {
            var user = db.Users.FirstOrDefault(u => u.Email == seed.Email);
            if (user is null) continue;

            if (db.UserCourses.Find(user.Id, Course1Id) is null)
            {
                pending.Add(new UserCourse(user.Id, Course1Id));
            }
        }

        if (pending.Count > 0)
        {
            db.UserCourses.AddRange(pending);
            db.SaveChanges();
        }
    }

    // ---- Seed-record shapes (kept local so the seeder is the only
    // place that has to know about them) ----

    private abstract record UserSeed(
        string Email,
        string FirstName,
        string LastName,
        string Gender,
        DateTime DateOfBirth);

    private sealed record StudentSeed(
        string LocalPart,
        string FirstName,
        string LastName,
        string Gender,
        DateTime DateOfBirth,
        CourseStatus CourseStatus,
        bool IsInternational)
        : UserSeed(
            Email: $"{LocalPart}@student.uts.edu.au",
            FirstName: FirstName,
            LastName: LastName,
            Gender: Gender,
            DateOfBirth: DateOfBirth);

    private sealed record TeacherSeed(
        string LocalPart,
        string FirstName,
        string LastName,
        string Gender,
        DateTime DateOfBirth,
        EmploymentStatus EmploymentStatus)
        : UserSeed(
            Email: $"{LocalPart}@faculty.uts.edu.au",
            FirstName: FirstName,
            LastName: LastName,
            Gender: Gender,
            DateOfBirth: DateOfBirth);
}
