using Database.Data;
using Database.Extensions;
using Database.Models;
using Database.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student4.Contracts;

namespace Database.Endpoints;

public static class PersistenceEndpoints
{
    public static IEndpointRouteBuilder MapPersistenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/internal/users");
        users.MapGet("/", GetUsers);
        users.MapGet("/{userId:guid}", GetUser);
        users.MapPost("/", CreateUser);
        users.MapPut("/{userId:guid}", UpdateUser);
        users.MapDelete("/{userId:guid}", DeleteUser);
        users.MapPost("/{userId:guid}/profile-summary", UpdateProfileSummary);

        var students = endpoints.MapGroup("/internal/students");
        students.MapGet("/{userId:guid}", GetStudent);
        students.MapPut("/{userId:guid}", UpdateStudent);

        var teachers = endpoints.MapGroup("/internal/teachers");
        teachers.MapGet("/{userId:guid}", GetTeacher);
        teachers.MapPut("/{userId:guid}", UpdateTeacher);

        var auth = endpoints.MapGroup("/internal/auth");
        auth.MapPost("/login", Login);
        auth.MapPost("/change-password", ChangePassword);
        auth.MapDelete("/delete-account", DeleteAccount);

        var reset = endpoints.MapGroup("/internal/password-reset-tokens");
        reset.MapPost("/", CreatePasswordResetToken);
        reset.MapPost("/lookup", GetPasswordResetToken);
        reset.MapPost("/redeem", RedeemPasswordResetToken);

        return endpoints;
    }

    #region Users
    private static async Task<IResult> GetUsers(AppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
        return Results.Ok(users.Select(u => u.ToRecord()));
    }

    private static async Task<IResult> GetUser(
        [FromRoute] Guid userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user.ToRecord());
    }

    private static async Task<IResult> CreateUser(
        AppDbContext db,
        [FromBody] CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        bool emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            return Results.Conflict("A user with this email already exists.");
        }

        if (!Enum.TryParse<UserType>(request.UserType, out var userType))
        {
            return Results.BadRequest($"Invalid user type: {request.UserType}");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.PasswordHash),
            FirstName = request.FirstName,
            MiddleNames = request.MiddleNames,
            LastName = request.LastName,
            Gender = Enum.Parse<Gender>(request.Gender),
            DateOfBirth = request.DateOfBirth,
            UserType = userType,
        };

        db.Users.Add(user);

        switch (userType)
        {
            case UserType.Student:
                if (request.Student is null)
                    return Results.BadRequest("Student details are required.");
                if (!Enum.TryParse<CourseStatus>(request.Student.CourseStatus, out var courseStatus))
                    return Results.BadRequest($"Invalid course status: {request.Student.CourseStatus}");
                db.Students.Add(new Student(user.Id)
                {
                    CourseStatus = courseStatus,
                    IsInternational = request.Student.IsInternational,
                    CanvasApiKey = request.Student.CanvasApiKey,
                });
                break;

            case UserType.Teacher:
                if (request.Teacher is null)
                    return Results.BadRequest("Teacher details are required.");
                if (!Enum.TryParse<EmploymentStatus>(request.Teacher.EmploymentStatus, out var employmentStatus))
                    return Results.BadRequest($"Invalid employment status: {request.Teacher.EmploymentStatus}");
                db.Teachers.Add(new Teacher(user.Id)
                {
                    EmploymentStatus = employmentStatus,
                    CanvasApiKey = request.Teacher.CanvasApiKey,
                });
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/internal/users/{user.Id}", user.ToRecord());
    }

    private static async Task<IResult> UpdateUser(
        [FromRoute] Guid userId,
        AppDbContext db,
        [FromBody] UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Results.NotFound();

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.MiddleNames = request.MiddleNames;
        user.LastName = request.LastName;
        user.Gender = Enum.Parse<Gender>(request.Gender);
        user.DateOfBirth = request.DateOfBirth;
        if (request.UserProfile is not null)
        {
            user.UserProfile = request.UserProfile;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(user.ToRecord());
    }

    private static async Task<IResult> DeleteUser(
        [FromRoute] Guid userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Results.NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateProfileSummary(
        [FromRoute] Guid userId,
        AppDbContext db,
        [FromBody] ProfileSummaryCommand request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Results.NotFound();

        user.UserProfile = request.Summary;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(user.ToRecord());
    }
    #endregion

    #region Students
    private static async Task<IResult> GetStudent(
        [FromRoute] Guid userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        return student is null ? Results.NotFound() : Results.Ok(student.ToRecord());
    }

    private static async Task<IResult> UpdateStudent(
        [FromRoute] Guid userId,
        AppDbContext db,
        [FromBody] UpdateStudentCommand request,
        CancellationToken cancellationToken)
    {
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (student is null)
        {
            var userExists = await db.Users
                .AnyAsync(u => u.Id == userId && u.UserType == UserType.Student, cancellationToken);
            if (!userExists)
                return Results.NotFound("User not found or is not a Student.");

            if (request.CourseStatus is null || !Enum.TryParse<CourseStatus>(request.CourseStatus, out var courseStatus))
                return Results.BadRequest("CourseStatus is required to create a student record.");
            student = new Student(userId)
            {
                CourseStatus = courseStatus,
                IsInternational = request.IsInternational ?? false,
                CanvasApiKey = request.CanvasApiKey ?? string.Empty,
            };
            db.Students.Add(student);
        }
        else
        {
            if (request.CourseStatus is not null && Enum.TryParse<CourseStatus>(request.CourseStatus, out var courseStatus))
                student.CourseStatus = courseStatus;
            if (request.IsInternational.HasValue)
                student.IsInternational = request.IsInternational.Value;
            if (request.CanvasApiKey is not null)
                student.CanvasApiKey = request.CanvasApiKey;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(student.ToRecord());
    }
    #endregion

    #region Teachers
    private static async Task<IResult> GetTeacher(
        [FromRoute] Guid userId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var teacher = await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
        return teacher is null ? Results.NotFound() : Results.Ok(teacher.ToRecord());
    }

    private static async Task<IResult> UpdateTeacher(
        [FromRoute] Guid userId,
        AppDbContext db,
        [FromBody] UpdateTeacherCommand request,
        CancellationToken cancellationToken)
    {
        var teacher = await db.Teachers.FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
        if (teacher is null)
        {
            var userExists = await db.Users
                .AnyAsync(u => u.Id == userId && u.UserType == UserType.Teacher, cancellationToken);
            if (!userExists)
                return Results.NotFound("User not found or is not a Teacher.");

            if (request.EmploymentStatus is null || !Enum.TryParse<EmploymentStatus>(request.EmploymentStatus, out var employmentStatus))
                return Results.BadRequest("EmploymentStatus is required to create a teacher record.");
            teacher = new Teacher(userId)
            {
                EmploymentStatus = employmentStatus,
                CanvasApiKey = request.CanvasApiKey ?? string.Empty,
            };
            db.Teachers.Add(teacher);
        }
        else
        {
            if (request.EmploymentStatus is not null && Enum.TryParse<EmploymentStatus>(request.EmploymentStatus, out var employmentStatus))
                teacher.EmploymentStatus = employmentStatus;
            if (request.CanvasApiKey is not null)
                teacher.CanvasApiKey = request.CanvasApiKey;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(teacher.ToRecord());
    }
    #endregion

    #region Auth
    private static async Task<IResult> Login(
        AppDbContext db,
        [FromBody] LoginCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required.");
        }

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(user.ToRecord());
    }

    private static async Task<IResult> ChangePassword(
        AppDbContext db,
        [FromBody] ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Results.BadRequest("Email, current password, and new password are required.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Password changed successfully." });
    }

    private static async Task<IResult> DeleteAccount(
        AppDbContext db,
        [FromBody] DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required to delete account.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Account deleted successfully." });
    }
    #endregion

    #region Password reset
    private static async Task<IResult> CreatePasswordResetToken(
        AppDbContext db,
        [FromBody] CreatePasswordResetTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Results.BadRequest("UserId is required.");
        }
        if (string.IsNullOrWhiteSpace(request.TokenHash))
        {
            return Results.BadRequest("TokenHash is required.");
        }

        var userExists = await db.Users
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return Results.NotFound("User not found.");
        }

        await db.PasswordResetTokens
            .Where(t => t.UserId == request.UserId && t.UsedAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var entity = new PasswordResetToken
        {
            UserId = request.UserId,
            TokenHash = request.TokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = request.ExpiresAtUtc,
        };
        db.PasswordResetTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/internal/password-reset-tokens/{entity.Id}",
            new PasswordResetTokenRecord(
                entity.Id,
                entity.UserId,
                entity.CreatedAtUtc,
                entity.ExpiresAtUtc,
                entity.UsedAtUtc));
    }

    private static async Task<IResult> GetPasswordResetToken(
        AppDbContext db,
        [FromBody] RedeemPasswordResetTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenHash))
        {
            return Results.BadRequest("TokenHash is required.");
        }

        var token = await db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TokenHash == request.TokenHash,
                cancellationToken);

        if (token is null ||
            token.UsedAtUtc is not null ||
            token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Results.NotFound();
        }

        return Results.Ok(new PasswordResetTokenRecord(
            token.Id,
            token.UserId,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.UsedAtUtc));
    }

    private static async Task<IResult> RedeemPasswordResetToken(
        AppDbContext db,
        [FromBody] ResetPasswordWithTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TokenHash))
        {
            return Results.BadRequest("TokenHash is required.");
        }
        if (string.IsNullOrWhiteSpace(request.NewPasswordHash))
        {
            return Results.BadRequest("NewPasswordHash is required.");
        }

        // BCrypt the new password before persisting
        var newHash = PasswordHasher.Hash(request.NewPasswordHash);

        var token = await db.PasswordResetTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == request.TokenHash
                    && t.UsedAtUtc == null
                    && t.ExpiresAtUtc > DateTime.UtcNow,
                cancellationToken);
        if (token is null)
        {
            return Results.NotFound();
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == token.UserId, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        token.UsedAtUtc = DateTime.UtcNow;
        user.PasswordHash = newHash;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(user.ToRecord());
    }
    #endregion
}
