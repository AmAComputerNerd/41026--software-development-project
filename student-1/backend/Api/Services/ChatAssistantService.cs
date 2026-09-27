using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using ChatMessageEntity = Api.Models.ChatMessage;

namespace Api.Services;

public sealed partial class ChatAssistantService(
    AppDbContext db,
    IAiDigestService aiDigestService,
    IRagClient ragClient,
    ILogger<ChatAssistantService> logger) : IChatAssistantService
{
    public async Task<ChatMessageDetailDto> GenerateInitialDigestAsync(
        Guid studentId,
        Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == chatSessionId && s.StudentId == studentId, cancellationToken)
            ?? throw new InvalidOperationException($"Chat session {chatSessionId} not found for student {studentId}.");

        var unreadNotifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.StudentId == studentId && !n.IsRead)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        string summary;
        try
        {
            summary = await aiDigestService.GenerateDigestAsync(studentId, unreadNotifications, cancellationToken);
        }
        catch (Exception ex)
        {
            LogGatewayFallback(logger, ex);
            summary = unreadNotifications.Count == 0
                ? "You're all caught up! There are no unread notifications right now."
                : $"You have {unreadNotifications.Count} unread notification(s): " +
                  string.Join("; ", unreadNotifications.Take(3).Select(n => $"[{n.Type}] {n.Message}"));
        }

        var greetingContent = $"### ⚡ Notification Summary\n\n{summary}\n\n*How can I help you organize your tasks or answer questions about your courses today?*";

        var assistantMessage = new ChatMessageEntity
        {
            ChatSessionId = chatSessionId,
            Role = "assistant",
            Content = greetingContent,
            CreatedAtUtc = DateTime.UtcNow,
            Confidence = "HIGH"
        };

        db.ChatMessages.Add(assistantMessage);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new ChatMessageDetailDto(
            Id: assistantMessage.Id,
            ChatSessionId: assistantMessage.ChatSessionId,
            Role: assistantMessage.Role,
            Content: assistantMessage.Content,
            CreatedAtUtc: assistantMessage.CreatedAtUtc,
            CitationsJson: null,
            Confidence: assistantMessage.Confidence);
    }

    public async Task<ChatMessageDetailDto> SendMessageAsync(
        Guid studentId,
        Guid chatSessionId,
        string prompt,
        bool includeRag = true,
        CancellationToken cancellationToken = default)
    {
        var session = await db.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == chatSessionId && s.StudentId == studentId, cancellationToken)
            ?? throw new InvalidOperationException($"Chat session {chatSessionId} not found for student {studentId}.");

        var userMessage = new ChatMessageEntity
        {
            ChatSessionId = chatSessionId,
            Role = "user",
            Content = prompt,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ChatMessages.Add(userMessage);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        // Fetch recent conversation history
        var historyMessages = await db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ChatSessionId == chatSessionId && m.Id != userMessage.Id)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(10)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new ChatMessageDto(m.Role, m.Content))
            .ToListAsync(cancellationToken);

        // Fetch unread notifications for context
        var unreadNotifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.StudentId == studentId && !n.IsRead)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        RagQueryResponseDto? ragResult = null;
        if (includeRag)
        {
            try
            {
                ragResult = await ragClient.QueryAsync(prompt, "student-1", cancellationToken);
            }
            catch (Exception ex)
            {
                LogRagFailed(logger, ex);
            }
        }

        string augmentedPrompt = prompt;
        if (ragResult is { HasSufficientContext: true } && !string.IsNullOrWhiteSpace(ragResult.Answer))
        {
            augmentedPrompt = $"{prompt}\n\n[Authoritative Knowledge Context from RAG: {ragResult.Answer}]";
        }

        string reply;
        try
        {
            reply = await aiDigestService.AskAssistantAsync(
                studentId,
                augmentedPrompt,
                historyMessages,
                unreadNotifications,
                cancellationToken);
        }
        catch (Exception ex)
        {
            LogGatewayFailed(logger, ex);
            reply = ragResult is { HasSufficientContext: true }
                ? $"Based on course documentation: {ragResult.Answer}"
                : "I am having trouble reaching the AI gateway right now. Please try again shortly.";
        }

        string? citationsJson = null;
        string? confidence = null;

        if (ragResult is { HasSufficientContext: true } && ragResult.Citations.Count > 0)
        {
            citationsJson = JsonSerializer.Serialize(ragResult.Citations);
            confidence = ragResult.Confidence;
        }

        var assistantMessage = new ChatMessageEntity
        {
            ChatSessionId = chatSessionId,
            Role = "assistant",
            Content = reply,
            CreatedAtUtc = DateTime.UtcNow,
            CitationsJson = citationsJson,
            Confidence = confidence
        };

        db.ChatMessages.Add(assistantMessage);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new ChatMessageDetailDto(
            Id: assistantMessage.Id,
            ChatSessionId: assistantMessage.ChatSessionId,
            Role: assistantMessage.Role,
            Content: assistantMessage.Content,
            CreatedAtUtc: assistantMessage.CreatedAtUtc,
            CitationsJson: assistantMessage.CitationsJson,
            Confidence: assistantMessage.Confidence);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to call AI Gateway for initial digest; falling back to direct summary.")]
    private static partial void LogGatewayFallback(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "RAG query failed; continuing with standard LLM completion.")]
    private static partial void LogRagFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to get AI completion from gateway.")]
    private static partial void LogGatewayFailed(ILogger logger, Exception exception);
}
