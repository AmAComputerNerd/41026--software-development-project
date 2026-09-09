using System.Net;
using System.Net.Http.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Integration;

public sealed class NotificationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNotifications_ReturnsOkWithFilteredResults()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notifications.AddRange(
                new Notification
                {
                    StudentId = studentId,
                    Type = NotificationType.Deadline,
                    SourceMicroservice = "student-3",
                    Message = "Assignment 1",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Notification
                {
                    StudentId = studentId,
                    Type = NotificationType.Grade,
                    SourceMicroservice = "student-5",
                    Message = "Quiz 1 graded",
                    IsRead = true,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Notification
                {
                    StudentId = Guid.NewGuid(),
                    Type = NotificationType.AI,
                    SourceMicroservice = "ai-mode",
                    Message = "Other student notification",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        // Act - filter by student and unread
        var response = await _client.GetAsync($"/notifications?studentId={studentId}&isRead=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("Assignment 1", notifications[0].Message);
        Assert.False(notifications[0].IsRead);
    }

    [Fact]
    public async Task GetNotification_ById_ReturnsExpectedNotification()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();
        Guid createdId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = new Notification
            {
                StudentId = studentId,
                Type = NotificationType.Account,
                SourceMicroservice = "student-4",
                Message = "Profile updated",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            createdId = notification.Id;
        }

        // Act
        var response = await _client.GetAsync($"/notifications/{createdId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notificationDto = await response.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.NotNull(notificationDto);
        Assert.Equal(createdId, notificationDto.Id);
        Assert.Equal("Profile updated", notificationDto.Message);
    }

    [Fact]
    public async Task GetNotification_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/notifications/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_And_MarkAsUnread_UpdatesState()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = new Notification
            {
                StudentId = Guid.NewGuid(),
                Type = NotificationType.Deadline,
                SourceMicroservice = "student-3",
                Message = "Deadline item",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            id = notification.Id;
        }

        // Act - Mark As Read
        var readResponse = await _client.PutAsync($"/notifications/{id}/read", null);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var readDto = await readResponse.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.NotNull(readDto);
        Assert.True(readDto.IsRead);

        // Act - Mark As Unread
        var unreadResponse = await _client.PutAsync($"/notifications/{id}/unread", null);
        Assert.Equal(HttpStatusCode.OK, unreadResponse.StatusCode);
        var unreadDto = await unreadResponse.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.NotNull(unreadDto);
        Assert.False(unreadDto.IsRead);
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllForStudentAsRead()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notifications.AddRange(
                new Notification
                {
                    StudentId = studentId,
                    Type = NotificationType.Deadline,
                    SourceMicroservice = "student-3",
                    Message = "Item 1",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Notification
                {
                    StudentId = studentId,
                    Type = NotificationType.Grade,
                    SourceMicroservice = "student-5",
                    Message = "Item 2",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PutAsync($"/notifications/read-all?studentId={studentId}", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var unreadCount = await db.Notifications.CountAsync(n => n.StudentId == studentId && !n.IsRead);
            Assert.Equal(0, unreadCount);
        }
    }

    [Fact]
    public async Task DeleteNotification_RemovesFromDatabase()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notification = new Notification
            {
                StudentId = Guid.NewGuid(),
                Type = NotificationType.AI,
                SourceMicroservice = "ai-mode",
                Message = "Temporary notification",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();
            id = notification.Id;
        }

        // Act
        var response = await _client.DeleteAsync($"/notifications/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var exists = await db.Notifications.AnyAsync(n => n.Id == id);
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task PushNotification_ValidRequest_CreatesAndReturnsCreated()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var request = new PushNotificationRequestDto(
            StudentId: studentId,
            Type: "Deadline",
            SourceMicroservice: "student-3-backend",
            Message: "You have an assignment due tomorrow at 5 PM.",
            RelatedEntityType: "Assignment",
            RelatedEntityId: entityId,
            ActionPayload: "{\"taskId\":\"t-100\"}"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/push", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<NotificationDto>();
        Assert.NotNull(created);
        Assert.Equal(studentId, created.StudentId);
        Assert.Equal("Deadline", created.Type);
        Assert.Equal("You have an assignment due tomorrow at 5 PM.", created.Message);
    }

    [Fact]
    public async Task PushNotification_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var request = new PushNotificationRequestDto(
            StudentId: Guid.NewGuid(),
            Type: "InvalidType123",
            SourceMicroservice: "student-3-backend",
            Message: "Hello",
            RelatedEntityType: null,
            RelatedEntityId: null,
            ActionPayload: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/notifications/push", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
