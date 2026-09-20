namespace McpServer.Configuration;

public sealed class Student3Options
{
    public const string SectionName = "Student3";

    public string BaseUrl { get; init; } = "http://student-3-backend:8080";
}
