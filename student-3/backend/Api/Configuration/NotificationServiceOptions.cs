namespace Api.Configuration;

public sealed class NotificationServiceOptions
{
    public const string SectionName = "NotificationService";

    public string BaseUrl { get; init; } = string.Empty;
}
