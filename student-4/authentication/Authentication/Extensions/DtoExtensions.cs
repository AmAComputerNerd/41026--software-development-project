using Authentication.DTOs;
using Student4.Contracts;

namespace Authentication.Extensions;

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
