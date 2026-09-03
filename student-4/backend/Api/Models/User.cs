namespace Api.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public string? MiddleNames { get; set; }
    public required string LastName { get; set; }
    public Gender Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public UserType UserType { get; set; }

    // AI-generated (or user-edited) profile summary. Populated by the
    // /api/users/{id}/profile-summary endpoint, or set directly by the
    // user when they edit their profile.
    public string? UserProfile { get; set; }

    public User()
    {
        Id = Guid.NewGuid();
    }
}

public enum Gender
{
    Male,
    Female,
    NonBinary,
}

public enum UserType
{
    Student,
    Teacher,
    Admin
}