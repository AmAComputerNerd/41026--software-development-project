namespace Api.Configuration;

public sealed class DatabaseServiceOptions
{
    public const string SectionName = "DatabaseService";

    public string BaseUrl { get; init; } = string.Empty;
}
