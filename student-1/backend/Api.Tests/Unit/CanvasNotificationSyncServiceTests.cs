using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Api.Tests.Unit;

public sealed class CanvasNotificationSyncServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly Mock<ISharedCanvasClient> _canvasClientMock;
    private readonly Mock<INotificationStreamBroker> _brokerMock;
    private readonly CanvasNotificationSyncService _syncService;

    public CanvasNotificationSyncServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        _canvasClientMock = new Mock<ISharedCanvasClient>();
        _brokerMock = new Mock<INotificationStreamBroker>();
        _syncService = new CanvasNotificationSyncService(
            _canvasClientMock.Object,
            _dbContext,
            _brokerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SyncAsync_WhenNewAssignmentDiscovered_CreatesNotificationAndWatermark()
    {
        // Arrange
        var courses = new List<SharedCanvasCourseDto>
        {
            new(101, "Software Architecture", "41026", "available")
        };

        var dueDate = DateTime.UtcNow.AddDays(7);
        var assignments = new List<SharedCanvasAssignmentDto>
        {
            new(
                Id: 501,
                CourseId: 101,
                Name: "Sprint 1 Submission",
                Description: "Submit deliverables",
                DueAt: dueDate,
                UpdatedAt: DateTime.UtcNow,
                WorkflowState: "published",
                Published: true,
                Submission: null
            )
        };

        _canvasClientMock
            .Setup(c => c.GetCoursesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(courses);

        _canvasClientMock
            .Setup(c => c.GetAssignmentsAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        // Act
        var result = await _syncService.SyncAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.NotificationsCreated);

        var notification = await _dbContext.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Deadline, notification.Type);
        Assert.Equal("canvas-sync", notification.SourceMicroservice);
        Assert.Contains("Sprint 1 Submission", notification.Message);

        var watermark = await _dbContext.CanvasAssignmentWatermarks.FirstOrDefaultAsync(w => w.CanvasAssignmentId == 501);
        Assert.NotNull(watermark);
        Assert.Equal(dueDate, watermark.LastDueDate);

        _brokerMock.Verify(b => b.PublishAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_WhenDueDateChanges_CreatesUpdatedNotification()
    {
        // Arrange
        var initialDueDate = DateTime.UtcNow.AddDays(3);
        var watermark = new CanvasAssignmentWatermark
        {
            CanvasAssignmentId = 601,
            LastDueDate = initialDueDate,
            LastWorkflowState = "published",
            LastSeenAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.CanvasAssignmentWatermarks.Add(watermark);
        await _dbContext.SaveChangesAsync();

        var courses = new List<SharedCanvasCourseDto> { new(102, "Cloud Computing", "41027", "available") };
        var newDueDate = DateTime.UtcNow.AddDays(5);
        var assignments = new List<SharedCanvasAssignmentDto>
        {
            new(
                Id: 601,
                CourseId: 102,
                Name: "Cloud Lab 2",
                Description: null,
                DueAt: newDueDate,
                UpdatedAt: DateTime.UtcNow,
                WorkflowState: "published",
                Published: true,
                Submission: null
            )
        };

        _canvasClientMock.Setup(c => c.GetCoursesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courses);
        _canvasClientMock.Setup(c => c.GetAssignmentsAsync(102, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);

        // Act
        var result = await _syncService.SyncAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.NotificationsCreated);

        var notification = await _dbContext.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Contains("Due date for \"Cloud Lab 2\" changed", notification.Message);

        var updatedWatermark = await _dbContext.CanvasAssignmentWatermarks.FirstOrDefaultAsync(w => w.CanvasAssignmentId == 601);
        Assert.NotNull(updatedWatermark);
        Assert.Equal(newDueDate, updatedWatermark.LastDueDate);
    }

    [Fact]
    public async Task SyncAsync_WhenSubmissionStateTransitionsToSubmitted_CreatesGradeNotification()
    {
        // Arrange
        var watermark = new CanvasAssignmentWatermark
        {
            CanvasAssignmentId = 701,
            LastDueDate = DateTime.UtcNow.AddDays(2),
            LastWorkflowState = "published",
            LastSubmissionState = "unsubmitted",
            LastSeenAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        _dbContext.CanvasAssignmentWatermarks.Add(watermark);
        await _dbContext.SaveChangesAsync();

        var courses = new List<SharedCanvasCourseDto> { new(103, "Algorithms", "31251", "available") };
        var assignments = new List<SharedCanvasAssignmentDto>
        {
            new(
                Id: 701,
                CourseId: 103,
                Name: "Assignment 2",
                Description: null,
                DueAt: watermark.LastDueDate,
                UpdatedAt: DateTime.UtcNow,
                WorkflowState: "published",
                Published: true,
                Submission: new SharedCanvasSubmissionDto("submitted", DateTime.UtcNow, false, false)
            )
        };

        _canvasClientMock.Setup(c => c.GetCoursesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courses);
        _canvasClientMock.Setup(c => c.GetAssignmentsAsync(103, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);

        // Act
        var result = await _syncService.SyncAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, result.NotificationsCreated);

        var notification = await _dbContext.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Grade, notification.Type);
        Assert.Contains("\"Assignment 2\" is now submitted.", notification.Message);
    }

    [Fact]
    public async Task SyncAsync_WhenNoChangesOccur_CreatesZeroNotifications()
    {
        // Arrange
        var dueDate = DateTime.UtcNow.AddDays(2);
        var watermark = new CanvasAssignmentWatermark
        {
            CanvasAssignmentId = 801,
            LastDueDate = dueDate,
            LastWorkflowState = "published",
            LastSubmissionState = "unsubmitted",
            LastSeenAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.CanvasAssignmentWatermarks.Add(watermark);
        await _dbContext.SaveChangesAsync();

        var courses = new List<SharedCanvasCourseDto> { new(104, "Data Science", "31252", "available") };
        var assignments = new List<SharedCanvasAssignmentDto>
        {
            new(
                Id: 801,
                CourseId: 104,
                Name: "Data Project",
                Description: null,
                DueAt: dueDate,
                UpdatedAt: DateTime.UtcNow,
                WorkflowState: "published",
                Published: true,
                Submission: new SharedCanvasSubmissionDto("unsubmitted", null, false, false)
            )
        };

        _canvasClientMock.Setup(c => c.GetCoursesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(courses);
        _canvasClientMock.Setup(c => c.GetAssignmentsAsync(104, It.IsAny<CancellationToken>())).ReturnsAsync(assignments);

        // Act
        var result = await _syncService.SyncAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result.NotificationsCreated);
        Assert.Empty(_dbContext.Notifications);
        _brokerMock.Verify(b => b.PublishAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
