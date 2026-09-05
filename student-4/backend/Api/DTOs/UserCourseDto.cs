namespace Api.Models;

public record UserCourseDto(
    Guid UserId,
    Guid CourseId
);
