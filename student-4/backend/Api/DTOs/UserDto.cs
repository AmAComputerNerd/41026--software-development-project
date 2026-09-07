using System.Text.Json.Serialization;

namespace Api.DTOs;

// UserDto is what we hand back to clients. PasswordHash is intentionally
// [JsonIgnore]-ed so it never leaves the server. The contract record
// carries the password hash from the database service for the login
// path, but the public DTO never serializes it.
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

    // AI-generated (or user-edited) profile summary. Nullable so the
    // field is optional — a user without a summary just has null.
    public string? UserProfile { get; init; }
}
