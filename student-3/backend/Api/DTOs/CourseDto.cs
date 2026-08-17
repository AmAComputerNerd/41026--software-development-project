namespace Api.DTOs;

public record CourseDto(
    Guid Id,
    string Code,
    string Name
);