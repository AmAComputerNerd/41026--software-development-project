namespace McpServer.Configuration;

public sealed class Student1Options
{
    public const string SectionName = "Student1";

    public string BaseUrl { get; init; } = "http://localhost:5101";
}
