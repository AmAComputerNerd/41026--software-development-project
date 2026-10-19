namespace MultiAgentServer.Configuration;

public sealed class MultiAgentOptions
{
    public const string SectionName = "MultiAgent";

    public string AiGatewayBaseUrl { get; set; } = "http://127.0.0.1:5001";

    public string McpBaseUrl { get; set; } = "http://127.0.0.1:5002";

    public string RagBaseUrl { get; set; } = "http://127.0.0.1:5003";

    public string Student1BaseUrl { get; set; } = "http://127.0.0.1:5101";

    public string Student2BaseUrl { get; set; } = "http://127.0.0.1:5102";

    public string Student3BaseUrl { get; set; } = "http://127.0.0.1:5103";

    public string Student4BaseUrl { get; set; } = "http://127.0.0.1:5104";

    public string Student5BaseUrl { get; set; } = "http://127.0.0.1:5105";

    public string DataFilePath { get; set; } = "data/workflows.json";
}
