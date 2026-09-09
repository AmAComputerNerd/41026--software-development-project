using System.Net;
using System.Text;
using System.Text.Json;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Api.Tests.Unit;

public sealed class OpenRouterDigestServiceTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }

    [Fact]
    public async Task GenerateDigestAsync_WhenGatewayReturnsSuccess_ReturnsContent()
    {
        // Arrange
        var jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "Good morning! You have 2 assignments due this week."
                    },
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        }));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-mode:8080/") };
        var service = new OpenRouterDigestService(httpClient, NullLogger<OpenRouterDigestService>.Instance);

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Type = NotificationType.Deadline,
                SourceMicroservice = "student-3",
                Message = "Lab 1 due tomorrow",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        // Act
        var result = await service.GenerateDigestAsync(Guid.NewGuid(), notifications);

        // Assert
        Assert.Equal("Good morning! You have 2 assignments due this week.", result);
    }

    [Fact]
    public async Task GenerateDigestAsync_WhenContentEmptyButReasoningPresent_ReturnsReasoning()
    {
        // Arrange
        var jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "",
                        "reasoning": "Fallback summary from model reasoning."
                    },
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        var handler = new MockHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        }));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-mode:8080/") };
        var service = new OpenRouterDigestService(httpClient, NullLogger<OpenRouterDigestService>.Instance);

        // Act
        var result = await service.GenerateDigestAsync(Guid.NewGuid(), []);

        // Assert
        Assert.Equal("Fallback summary from model reasoning.", result);
    }

    [Fact]
    public async Task GenerateDigestAsync_WhenGatewayReturns500_ThrowsAiGatewayException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server Error")
        }));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-mode:8080/") };
        var service = new OpenRouterDigestService(httpClient, NullLogger<OpenRouterDigestService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<AiGatewayException>(() =>
            service.GenerateDigestAsync(Guid.NewGuid(), []));
    }

    [Fact]
    public async Task AskAssistantAsync_WhenCalled_SendsConversationAndReturnsReply()
    {
        // Arrange
        var jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "You should focus on the 41026 sprint submission first."
                    },
                    "finish_reason": "stop"
                }
            ]
        }
        """;

        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-mode:8080/") };
        var service = new OpenRouterDigestService(httpClient, NullLogger<OpenRouterDigestService>.Instance);

        var history = new List<ChatMessageDto>
        {
            new("user", "What should I do first?"),
            new("assistant", "Let me check your deadlines.")
        };

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Type = NotificationType.Deadline,
                SourceMicroservice = "student-3",
                Message = "41026 sprint submission due in 2 days",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        // Act
        var reply = await service.AskAssistantAsync(
            Guid.NewGuid(),
            "Which one is highest priority?",
            history,
            notifications);

        // Assert
        Assert.Equal("You should focus on the 41026 sprint submission first.", reply);
        Assert.NotNull(capturedRequest);
        var requestBody = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("41026 sprint submission", requestBody);
        Assert.Contains("Which one is highest priority?", requestBody);
    }
}
