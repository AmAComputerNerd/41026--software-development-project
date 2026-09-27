using Api.Configuration;
using Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Api.Tests.Unit;

public class McpRagClientDisabledTests
{
    [Fact]
    public async Task McpClient_WhenDisabled_ReturnsDisabledStatus()
    {
        var options = Options.Create(new McpServerOptions
        {
            Enabled = false,
            BaseUrl = "http://localhost:5002/mcp"
        });

        var client = new McpClient(options);
        var result = await client.BroadcastAlertAsync(
            Guid.NewGuid(),
            "Title",
            "Message text here",
            "Medium");

        Assert.False(result.Success);
        Assert.Equal("disabled", result.Status);
        Assert.Contains("disabled in configuration", result.Error);
    }

    [Fact]
    public async Task RagClient_WhenDisabled_ReturnsDisabledStatus()
    {
        var options = Options.Create(new RagServerOptions
        {
            Enabled = false,
            BaseUrl = "http://localhost:5003"
        });

        using var httpClient = new HttpClient();
        var client = new RagClient(httpClient, options);
        var result = await client.QueryAsync("What is the late penalty?");

        Assert.False(result.HasSufficientContext);
        Assert.Equal("DISABLED", result.Confidence);
        Assert.Contains("disabled in configuration", result.Answer);
    }
}
