namespace Api.DTOs;

public record GenerateTaskBreakdownRequestDto(
    string Prompt,
    string Priority
);

public record GenerateTaskDescriptionRequestDto(
    string Title,
    Guid? CourseId,
    Guid? ParentTaskId
);

public record GeneratedTaskDescriptionDto(string Description);
