using System.Net;
using System.Net.Http.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Integration;

public sealed class PreferenceEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PreferenceEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPreferences_ReturnsAllPreferencesForStudent()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NotificationPreferences.AddRange(
                new NotificationPreference
                {
                    StudentId = studentId,
                    Type = NotificationType.Deadline,
                    Channel = NotificationChannel.Email,
                    Enabled = true,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new NotificationPreference
                {
                    StudentId = studentId,
                    Type = NotificationType.Grade,
                    Channel = NotificationChannel.InApp,
                    Enabled = false,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new NotificationPreference
                {
                    StudentId = Guid.NewGuid(),
                    Type = NotificationType.AI,
                    Channel = NotificationChannel.Email,
                    Enabled = true,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/preferences?studentId={studentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preferences = await response.Content.ReadFromJsonAsync<List<NotificationPreferenceDto>>();
        Assert.NotNull(preferences);
        Assert.Equal(2, preferences.Count);
    }

    [Fact]
    public async Task CreatePreference_ValidDto_CreatesAndReturnsCreated()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();
        var request = new NotificationPreferenceRequestDto(
            StudentId: studentId,
            Type: "Account",
            Channel: "Email",
            Enabled: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/preferences", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>();
        Assert.NotNull(created);
        Assert.Equal(studentId, created.StudentId);
        Assert.Equal("Account", created.Type);
        Assert.Equal("Email", created.Channel);
        Assert.True(created.Enabled);
    }

    [Fact]
    public async Task CreatePreference_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var request = new NotificationPreferenceRequestDto(
            StudentId: Guid.NewGuid(),
            Type: "InvalidType",
            Channel: "Email",
            Enabled: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/preferences", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePreference_ValidDto_UpdatesAndReturnsOk()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        var studentId = Guid.NewGuid();
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pref = new NotificationPreference
            {
                StudentId = studentId,
                Type = NotificationType.Deadline,
                Channel = NotificationChannel.Email,
                Enabled = true,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.NotificationPreferences.Add(pref);
            await db.SaveChangesAsync();
            id = pref.Id;
        }

        var updateRequest = new NotificationPreferenceRequestDto(
            StudentId: studentId,
            Type: "Deadline",
            Channel: "Email",
            Enabled: false
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/preferences/{id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>();
        Assert.NotNull(updated);
        Assert.False(updated.Enabled);
    }

    [Fact]
    public async Task DeletePreference_RemovesPreference()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pref = new NotificationPreference
            {
                StudentId = Guid.NewGuid(),
                Type = NotificationType.Grade,
                Channel = NotificationChannel.InApp,
                Enabled = true,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.NotificationPreferences.Add(pref);
            await db.SaveChangesAsync();
            id = pref.Id;
        }

        // Act
        var response = await _client.DeleteAsync($"/preferences/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var exists = await db.NotificationPreferences.AnyAsync(p => p.Id == id);
            Assert.False(exists);
        }
    }
}
