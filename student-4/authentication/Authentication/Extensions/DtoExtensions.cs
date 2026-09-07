using Authentication.DTOs;
using Student4.Contracts;

namespace Authentication.Extensions;

// Converts contract records (returned by the database service) to
// the public DTOs this service exposes. This is the boundary
// between the internal persistence shape and the public contract.
public static class DtoExtensions
{
    public static UserDto ToDto(this UserRecord record)
    {
        return new UserDto
        {
            Id = record.Id,
            Email = record.Email,
            PasswordHash = record.PasswordHash,
            FirstName = record.FirstName,
            MiddleNames = record.MiddleNames,
            LastName = record.LastName,
            Gender = record.Gender,
            DateOfBirth = record.DateOfBirth,
            UserType = record.UserType,
            UserProfile = record.UserProfile,
        };
    }
}
