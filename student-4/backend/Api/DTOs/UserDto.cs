using System.Text.Json.Serialization;

namespace Api.DTOs;

public class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;
    public string? MiddleNames { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string Gender { get; init; } = "Male";
    public DateTime DateOfBirth { get; init; }
    public string UserType { get; init; } = "Student";

    public string? UserProfile { get; init; }
}
