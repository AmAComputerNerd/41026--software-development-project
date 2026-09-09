using Api.DTOs;
using Api.Extensions;
using Api.Models;
using Xunit;

namespace Api.Tests.Unit;

public sealed class DtoExtensionsTests
{
    [Fact]
    public void NotificationToDto_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var entityId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = id,
            StudentId = studentId,
            Type = NotificationType.Deadline,
            SourceMicroservice = "student-3-backend",
            Message = "Assignment 1 is due tomorrow",
            IsRead = false,
            CreatedAtUtc = createdAt,
            RelatedEntityType = "Assignment",
            RelatedEntityId = entityId,
            ActionPayload = "{\"taskId\": \"t-1\"}"
        };

        // Act
        var dto = notification.ToDto();

        // Assert
        Assert.Equal(id, dto.Id);
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal("Deadline", dto.Type);
        Assert.Equal("student-3-backend", dto.SourceMicroservice);
        Assert.Equal("Assignment 1 is due tomorrow", dto.Message);
        Assert.False(dto.IsRead);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal("Assignment", dto.RelatedEntityType);
        Assert.Equal(entityId, dto.RelatedEntityId);
        Assert.Equal("{\"taskId\": \"t-1\"}", dto.ActionPayload);
    }

    [Fact]
    public void NotificationPreferenceToDto_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;

        var preference = new NotificationPreference
        {
            Id = id,
            StudentId = studentId,
            Type = NotificationType.Grade,
            Channel = NotificationChannel.InApp,
            Enabled = true,
            UpdatedAtUtc = updatedAt
        };

        // Act
        var dto = preference.ToDto();

        // Assert
        Assert.Equal(id, dto.Id);
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal("Grade", dto.Type);
        Assert.Equal("InApp", dto.Channel);
        Assert.True(dto.Enabled);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
    }

    [Fact]
    public void AiDigestToDto_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;

        var digest = new AiDigest
        {
            Id = id,
            StudentId = studentId,
            Summary = "Here is your morning digest.",
            GeneratedAtUtc = generatedAt
        };

        // Act
        var dto = digest.ToDto();

        // Assert
        Assert.Equal(id, dto.Id);
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal("Here is your morning digest.", dto.Summary);
        Assert.Equal(generatedAt, dto.GeneratedAtUtc);
    }
}
