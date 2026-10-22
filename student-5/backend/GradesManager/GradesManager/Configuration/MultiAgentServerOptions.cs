namespace GradesManager.Configuration
{
    public sealed class MultiAgentServerOptions
    {
        public const string SectionName = "MultiAgentServer";

        public bool? Enabled { get; init; }

        public string BaseUrl { get; init; } = string.Empty;
    }
}
