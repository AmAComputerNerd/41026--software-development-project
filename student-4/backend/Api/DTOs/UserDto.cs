using System.Text.Json.Serialization;
using Api.Models;

namespace Api.DTOs;

// UserDto is what we hand back to clients. PasswordHash is intentionally
// [JsonIgnore]-ed so it never leaves the server — the constructor still
// accepts it so CreateUser can persist it, and ToDto() on the model
// still passes it through.
public class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;
    public string? MiddleNames { get; init; }
    public string LastName { get; init; } = string.Empty;
    public Gender Gender { get; init; }
    public DateTime DateOfBirth { get; init; }
    public UserType UserType { get; init; }

    // AI-generated (or user-edited) profile summary. Nullable so the
    // field is optional — a user without a summary just has null.
    public string? UserProfile { get; init; }
}
