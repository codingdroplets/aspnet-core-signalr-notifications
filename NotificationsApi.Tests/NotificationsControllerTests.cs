using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NotificationsApi.Controllers;
using Xunit;

namespace NotificationsApi.Tests;

/// <summary>
/// Integration tests for the NotificationsController REST endpoints.
/// Uses WebApplicationFactory to spin up the full ASP.NET Core pipeline in-process.
/// </summary>
public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/notifications/status ─────────────────────────────────────────

    [Fact]
    public async Task GetStatus_ReturnsOk_WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<StatusResponse>();
        Assert.NotNull(result);
        Assert.Equal("healthy", result.Status);
        Assert.Equal("/hubs/notifications", result.HubEndpoint);
    }

    [Fact]
    public async Task GetStatus_ReturnsContentTypeJson()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/status");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ── POST /api/notifications/broadcast ─────────────────────────────────────

    [Fact]
    public async Task Broadcast_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new BroadcastRequest
        {
            Title = "Test Notification",
            Body = "This is a test broadcast message.",
            Severity = "info"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BroadcastResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.NotificationId);
        Assert.Equal("all-clients", result.Target);
    }

    [Fact]
    public async Task Broadcast_WithDefaultSeverity_ReturnsOk()
    {
        // Arrange — no Severity field supplied (optional field)
        var request = new { Title = "Alert", Body = "System maintenance in 10 minutes." };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Broadcast_MissingTitle_ReturnsBadRequest()
    {
        // Arrange — Title is required
        var request = new { Body = "No title provided." };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert: model validation should reject missing required field
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Broadcast_MissingBody_ReturnsBadRequest()
    {
        // Arrange — Body is required
        var request = new { Title = "Missing body" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Broadcast_TitleExceedsMaxLength_ReturnsBadRequest()
    {
        // Arrange — Title max is 200 chars
        var request = new BroadcastRequest
        {
            Title = new string('A', 201),
            Body = "Body text"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── POST /api/notifications/topic/{topic} ─────────────────────────────────

    [Fact]
    public async Task SendToTopic_WithValidTopicAndRequest_ReturnsOk()
    {
        // Arrange
        var request = new BroadcastRequest
        {
            Title = "Order Shipped",
            Body = "Your order #12345 has been shipped.",
            Severity = "success"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/topic/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<BroadcastResult>();
        Assert.NotNull(result);
        Assert.Equal("orders", result.Target);
        Assert.NotEmpty(result.NotificationId);
    }

    [Fact]
    public async Task SendToTopic_WithDifferentTopics_AllReturnOk()
    {
        // Arrange — Test multiple topics
        var topics = new[] { "orders", "alerts", "payments", "chat-general" };
        var request = new BroadcastRequest
        {
            Title = "Test",
            Body = "Topic routing test"
        };

        foreach (var topic in topics)
        {
            // Act
            var response = await _client.PostAsJsonAsync($"/api/notifications/topic/{topic}", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<BroadcastResult>();
            Assert.NotNull(result);
            Assert.Equal(topic, result.Target);
        }
    }

    [Fact]
    public async Task SendToTopic_NotificationIdIsUniquePerRequest()
    {
        // Arrange
        var request = new BroadcastRequest
        {
            Title = "Unique ID test",
            Body = "Each notification should have a unique ID"
        };

        // Act — two separate requests
        var response1 = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);
        var response2 = await _client.PostAsJsonAsync("/api/notifications/broadcast", request);

        // Assert — IDs must differ
        var result1 = await response1.Content.ReadFromJsonAsync<BroadcastResult>();
        var result2 = await response2.Content.ReadFromJsonAsync<BroadcastResult>();

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotEqual(result1.NotificationId, result2.NotificationId);
    }
}
