namespace Api.Configuration;

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public string BaseUrl { get; init; } = string.Empty;
}
