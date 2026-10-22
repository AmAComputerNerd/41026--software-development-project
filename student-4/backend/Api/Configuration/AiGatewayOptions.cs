namespace Api.Configuration;

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public bool? Enabled { get; init; }

    public string BaseUrl { get; init; } = string.Empty;
}
