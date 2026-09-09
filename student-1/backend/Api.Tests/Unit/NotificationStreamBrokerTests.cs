using Api.DTOs;
using Api.Services;
using Xunit;

namespace Api.Tests.Unit;

public sealed class NotificationStreamBrokerTests
{
    [Fact]
    public async Task PublishAsync_DeliversNotificationToSubscriber()
    {
        // Arrange
        var broker = new NotificationStreamBroker();
        var studentId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var reader = broker.Subscribe(studentId, cts.Token);

        var notification = new NotificationDto(
            Id: Guid.NewGuid(),
            StudentId: studentId,
            Type: "Deadline",
            SourceMicroservice: "test",
            Message: "Test deadline",
            IsRead: false,
            CreatedAtUtc: DateTime.UtcNow,
            RelatedEntityType: null,
            RelatedEntityId: null,
            ActionPayload: null
        );

        // Act
        await broker.PublishAsync(notification);

        // Assert
        Assert.True(reader.TryRead(out var received));
        Assert.NotNull(received);
        Assert.Equal(notification.Id, received.Id);
        Assert.Equal("Test deadline", received.Message);
    }

    [Fact]
    public async Task PublishAsync_DeliversToMultipleSubscribersForSameStudent()
    {
        // Arrange
        var broker = new NotificationStreamBroker();
        var studentId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var reader1 = broker.Subscribe(studentId, cts.Token);
        var reader2 = broker.Subscribe(studentId, cts.Token);

        var notification = new NotificationDto(
            Id: Guid.NewGuid(),
            StudentId: studentId,
            Type: "Grade",
            SourceMicroservice: "test",
            Message: "Grade posted",
            IsRead: false,
            CreatedAtUtc: DateTime.UtcNow,
            RelatedEntityType: null,
            RelatedEntityId: null,
            ActionPayload: null
        );

        // Act
        await broker.PublishAsync(notification);

        // Assert
        Assert.True(reader1.TryRead(out var received1));
        Assert.True(reader2.TryRead(out var received2));
        Assert.Equal(notification.Id, received1!.Id);
        Assert.Equal(notification.Id, received2!.Id);
    }

    [Fact]
    public async Task PublishAsync_DoesNotDeliverToOtherStudents()
    {
        // Arrange
        var broker = new NotificationStreamBroker();
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var readerA = broker.Subscribe(studentA, cts.Token);
        var readerB = broker.Subscribe(studentB, cts.Token);

        var notificationA = new NotificationDto(
            Id: Guid.NewGuid(),
            StudentId: studentA,
            Type: "System",
            SourceMicroservice: "test",
            Message: "For student A only",
            IsRead: false,
            CreatedAtUtc: DateTime.UtcNow,
            RelatedEntityType: null,
            RelatedEntityId: null,
            ActionPayload: null
        );

        // Act
        await broker.PublishAsync(notificationA);

        // Assert
        Assert.True(readerA.TryRead(out var receivedA));
        Assert.Equal(notificationA.Id, receivedA!.Id);
        Assert.False(readerB.TryRead(out _));
    }

    [Fact]
    public async Task Cancellation_UnsubscribesAndCompletesChannel()
    {
        // Arrange
        var broker = new NotificationStreamBroker();
        var studentId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var reader = broker.Subscribe(studentId, cts.Token);

        // Act
        await cts.CancelAsync();

        // Channel should be completed or closed
        Assert.False(reader.TryRead(out _));

        var notification = new NotificationDto(
            Id: Guid.NewGuid(),
            StudentId: studentId,
            Type: "System",
            SourceMicroservice: "test",
            Message: "Should not be received",
            IsRead: false,
            CreatedAtUtc: DateTime.UtcNow,
            RelatedEntityType: null,
            RelatedEntityId: null,
            ActionPayload: null
        );

        // Publishing after cancellation shouldn't throw or deliver
        await broker.PublishAsync(notification);
        Assert.False(reader.TryRead(out _));
    }
}
