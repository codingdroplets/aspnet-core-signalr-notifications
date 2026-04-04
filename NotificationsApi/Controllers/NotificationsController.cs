using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationsApi.Hubs;

namespace NotificationsApi.Controllers;

/// <summary>
/// REST API controller for broadcasting server-side notifications via SignalR.
/// 
/// This represents the server-push pattern: your backend logic (a job, webhook, 
/// payment event, etc.) calls these endpoints to push notifications to all 
/// connected clients without any client-to-server message.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        ILogger<NotificationsController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts a notification to ALL connected clients.
    /// Use this for system-wide announcements.
    /// </summary>
    /// <param name="request">The notification to broadcast.</param>
    /// <returns>Details of the sent notification.</returns>
    [HttpPost("broadcast")]
    [ProducesResponseType(typeof(BroadcastResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Body = request.Body,
            Topic = "broadcast",
            SentAt = DateTime.UtcNow,
            Severity = request.Severity ?? "info"
        };

        // Push to all connected SignalR clients
        await _hubContext.Clients.All.ReceiveNotification(notification);

        _logger.LogInformation("Broadcast notification {NotificationId} sent to all clients", notification.Id);

        return Ok(new BroadcastResult
        {
            NotificationId = notification.Id,
            SentAt = notification.SentAt,
            Target = "all-clients",
            Message = "Notification broadcast to all connected clients."
        });
    }

    /// <summary>
    /// Sends a notification to all clients subscribed to a specific topic.
    /// Use this for targeted notifications (e.g., "orders", "alerts").
    /// </summary>
    /// <param name="topic">The topic/group name to target.</param>
    /// <param name="request">The notification to send.</param>
    /// <returns>Details of the sent notification.</returns>
    [HttpPost("topic/{topic}")]
    [ProducesResponseType(typeof(BroadcastResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendToTopic(
        [FromRoute] string topic,
        [FromBody] BroadcastRequest request)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return BadRequest(new { error = "Topic name cannot be empty." });
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Body = request.Body,
            Topic = topic,
            SentAt = DateTime.UtcNow,
            Severity = request.Severity ?? "info"
        };

        // Push only to clients in this topic group
        await _hubContext.Clients.Group(topic).ReceiveNotification(notification);

        _logger.LogInformation(
            "Notification {NotificationId} sent to topic '{Topic}'",
            notification.Id, topic);

        return Ok(new BroadcastResult
        {
            NotificationId = notification.Id,
            SentAt = notification.SentAt,
            Target = topic,
            Message = $"Notification sent to topic '{topic}'."
        });
    }

    /// <summary>
    /// Health check — returns service status. 
    /// Use GET /api/notifications/status to verify the API is running.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new StatusResponse
        {
            Status = "healthy",
            Service = "Notifications API (SignalR)",
            Timestamp = DateTime.UtcNow,
            HubEndpoint = "/hubs/notifications"
        });
    }
}

// ── Request/Response DTOs ─────────────────────────────────────────────────────

/// <summary>Request payload for broadcasting a notification.</summary>
public sealed record BroadcastRequest
{
    /// <summary>Short notification title (required, max 200 chars).</summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string Title { get; init; } = string.Empty;

    /// <summary>Full notification body (required, max 2000 chars).</summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(2000)]
    public string Body { get; init; } = string.Empty;

    /// <summary>Optional severity: info | warning | error | success. Defaults to "info".</summary>
    public string? Severity { get; init; }
}

/// <summary>Response returned after successfully broadcasting a notification.</summary>
public sealed record BroadcastResult
{
    public string NotificationId { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
    public string Target { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>Service health/status response.</summary>
public sealed record StatusResponse
{
    public string Status { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string HubEndpoint { get; init; } = string.Empty;
}
