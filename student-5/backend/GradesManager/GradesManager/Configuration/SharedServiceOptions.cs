namespace GradesManager.Configuration;

public sealed class SharedServiceOptions
{
    public const string SectionName = "SharedService";

    public string BaseUrl { get; init; } = string.Empty;
}