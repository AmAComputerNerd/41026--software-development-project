namespace Api.Configuration;

public sealed class CanvasOptions
{
    public const string SectionName = "Canvas";

    public string BaseUrl { get; init; } = string.Empty;
    public string ApiToken { get; init; } = string.Empty;
}
