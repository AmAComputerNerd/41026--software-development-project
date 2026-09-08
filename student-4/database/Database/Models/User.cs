namespace Database.Models;

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
