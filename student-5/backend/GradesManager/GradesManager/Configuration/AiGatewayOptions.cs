namespace GradesManager.Configuration
{
    public sealed class AiGatewayOptions
    {
        public const string SectionName = "AiGateway";
        public string BaseUrl { get; init; } = "http://ai-mode:8080";
    }
}
