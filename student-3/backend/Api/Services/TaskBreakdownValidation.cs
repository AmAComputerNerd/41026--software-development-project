namespace Api.Services;

public static class TaskBreakdownValidation
{
    public static bool IsValid(IReadOnlyList<GeneratedSubtask> tasks, int minimum = 2) =>
        tasks.Count >= minimum && tasks.Count <= 10 &&
        tasks.All(task => task is not null && !string.IsNullOrWhiteSpace(task.Title) &&
            task.Title.Length <= 120 && task.Description?.Length is not > 500) &&
        tasks.Select(task => task.Title).Distinct(StringComparer.OrdinalIgnoreCase).Count() == tasks.Count;
}
