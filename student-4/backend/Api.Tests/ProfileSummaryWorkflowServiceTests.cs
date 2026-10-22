using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.DTOs;
using Api.Services;
using Moq;
using Student4.Contracts;
using Xunit;

namespace Api.Tests;

public class ProfileSummaryWorkflowServiceTests
{
    private readonly Mock<IMultiAgentClient> _mockMultiAgentClient;
    private readonly Mock<IAiProfileSummaryService> _mockAiProfileSummaryService;
    private readonly ProfileSummaryWorkflowService _service;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly UserRecord _testUser;
    private readonly StudentRecord _testStudent;

    public ProfileSummaryWorkflowServiceTests()
    {
        _mockMultiAgentClient = new Mock<IMultiAgentClient>();
        _mockAiProfileSummaryService = new Mock<IAiProfileSummaryService>();
        _service = new ProfileSummaryWorkflowService(_mockMultiAgentClient.Object, _mockAiProfileSummaryService.Object);

        _testUser = new UserRecord(
            Id: _testUserId,
            Email: "test@example.com",
            PasswordHash: "hashed",
            FirstName: "John",
            MiddleNames: "Middle",
            LastName: "Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(2000, 1, 1),
            UserType: "Student",
            UserProfile: "Old summary"
        );

        _testStudent = new StudentRecord(
            UserId: _testUserId,
            CourseStatus: "FullTime",
            IsInternational: false,
            CanvasApiKey: "test-key"
        );
    }

    [Fact]
    public async Task GenerateAsync_WhenMultiAgentEnabledAndSucceeds_ReturnsProposalFromMultiAgent()
    {
        // Arrange
        _mockMultiAgentClient.Setup(x => x.IsEnabled).Returns(true);
        
        var expectedWorkflow = new MultiAgentWorkflowDto(
            Id: Guid.NewGuid(),
            Owner: "student-4",
            StudentId: _testUserId,
            Goal: "Test goal",
            Status: "AwaitingHumanReview",
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow,
            Planner: new MultiAgentPlannerDto("Objective", [], [], DateTime.UtcNow),
            Worker: new MultiAgentWorkerDto("Evidence", "Multi-Agent summary", [], [], DateTime.UtcNow),
            Reviewer: new MultiAgentReviewerDto("Approved", "High", "Good", [], [], DateTime.UtcNow),
            HumanReview: null,
            AuditLog: [],
            Revision: 0,
            Revisions: null
        );

        _mockMultiAgentClient
            .Setup(x => x.RunAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWorkflow);

        // Act
        var result = await _service.GenerateAsync(_testUser, _testStudent, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Multi-Agent summary", result.NewSummary);
        Assert.Equal(expectedWorkflow.Id, result.Workflow.Id);
        _mockMultiAgentClient.Verify(x => x.RunAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAiProfileSummaryService.Verify(x => x.GenerateSummaryAsync(It.IsAny<UserRecord>(), It.IsAny<StudentRecord?>(), It.IsAny<TeacherRecord?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_WhenMultiAgentDisabled_UsesAiModeFallback()
    {
        // Arrange
        _mockMultiAgentClient.Setup(x => x.IsEnabled).Returns(false);
        
        _mockAiProfileSummaryService
            .Setup(x => x.GenerateSummaryAsync(_testUser, _testStudent, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI Mode fallback summary");

        // Act
        var result = await _service.GenerateAsync(_testUser, _testStudent, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AI Mode fallback summary", result.NewSummary);
        Assert.Equal("Completed", result.Workflow.Status);
        _mockMultiAgentClient.Verify(x => x.RunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAiProfileSummaryService.Verify(x => x.GenerateSummaryAsync(_testUser, _testStudent, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WhenMultiAgentThrowsException_UsesAiModeFallback()
    {
        // Arrange
        _mockMultiAgentClient.Setup(x => x.IsEnabled).Returns(true);
        
        _mockMultiAgentClient
            .Setup(x => x.RunAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MultiAgentServiceException("Multi-Agent server unavailable"));

        _mockAiProfileSummaryService
            .Setup(x => x.GenerateSummaryAsync(_testUser, _testStudent, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI Mode fallback after exception");

        // Act
        var result = await _service.GenerateAsync(_testUser, _testStudent, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AI Mode fallback after exception", result.NewSummary);
        Assert.Equal("Completed", result.Workflow.Status);
        _mockMultiAgentClient.Verify(x => x.RunAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAiProfileSummaryService.Verify(x => x.GenerateSummaryAsync(_testUser, _testStudent, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WhenMultiAgentThrowsNonMultiAgentException_PropagatesException()
    {
        // Arrange
        _mockMultiAgentClient.Setup(x => x.IsEnabled).Returns(true);
        
        _mockMultiAgentClient
            .Setup(x => x.RunAsync(_testUserId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GenerateAsync(_testUser, _testStudent, null, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_WithTeacherUser_WorksCorrectly()
    {
        // Arrange
        _mockMultiAgentClient.Setup(x => x.IsEnabled).Returns(false);
        
        var teacherUser = new UserRecord(
            Id: _testUserId,
            Email: "teacher@example.com",
            PasswordHash: "hashed",
            FirstName: "Jane",
            MiddleNames: null,
            LastName: "Smith",
            Gender: "Female",
            DateOfBirth: new DateTime(1980, 1, 1),
            UserType: "Teacher",
            UserProfile: "Old teacher summary"
        );

        var teacherRecord = new TeacherRecord(
            UserId: _testUserId,
            EmploymentStatus: "FullTime",
            CanvasApiKey: "teacher-key"
        );

        _mockAiProfileSummaryService
            .Setup(x => x.GenerateSummaryAsync(teacherUser, null, teacherRecord, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Teacher summary from AI Mode");

        // Act
        var result = await _service.GenerateAsync(teacherUser, null, teacherRecord, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Teacher summary from AI Mode", result.NewSummary);
        _mockAiProfileSummaryService.Verify(x => x.GenerateSummaryAsync(teacherUser, null, teacherRecord, It.IsAny<CancellationToken>()), Times.Once);
    }
}