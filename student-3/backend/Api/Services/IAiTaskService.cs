namespace Api.Services;

public interface IAiTaskService
{
    Task<IReadOnlyList<GeneratedSubtask>> GenerateSubtasksAsync(
        AiTaskContext context,
        string prompt,
        CancellationToken cancellationToken);

    Task<string> GenerateDescriptionAsync(
        AiTaskContext context,
        CancellationToken cancellationToken);
}

public sealed record AiTaskContext(
    string Title,
    string? Description,
    string? CourseName,
    string? ParentTaskTitle,
    DateTime? DueDate);

public sealed record GeneratedSubtask(string Title, string? Description);
