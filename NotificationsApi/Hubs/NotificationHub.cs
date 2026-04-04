using Microsoft.AspNetCore.SignalR;

namespace NotificationsApi.Hubs;

/// <summary>
/// Typed interface defining the methods the server can invoke on connected clients.
/// Using a typed hub contract prevents typos and provides IntelliSense support.
/// </summary>
public interface INotificationClient
{
    /// <summary>Sends a notification message to the client.</summary>
    Task ReceiveNotification(NotificationMessage message);

    /// <summary>Confirms to the client that their message was received.</summary>
    Task ReceiveAcknowledgement(string notificationId);
}

/// <summary>
/// SignalR Hub for broadcasting real-time notifications.
/// 
/// Connection lifecycle:
///   - OnConnectedAsync    → client joins the "all-clients" group
///   - OnDisconnectedAsync → client is removed from all groups automatically
/// 
/// Client groups allow targeting broadcasts to subsets of users (e.g. per-user,
/// per-role, or per-topic channels).
/// </summary>
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client establishes a WebSocket/long-poll connection.
    /// Adds the client to the "all-clients" broadcast group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-clients");
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects (gracefully or due to an error).
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected: {ConnectionId}. Reason: {Reason}",
            Context.ConnectionId,
            exception?.Message ?? "Clean disconnect");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Called by clients to subscribe to a specific notification topic (group).
    /// E.g. "orders", "alerts", "chat-room-1"
    /// </summary>
    /// <param name="topic">The group/topic name to subscribe to.</param>
    public async Task SubscribeToTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new HubException("Topic name cannot be empty.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, topic);
        _logger.LogInformation("Client {ConnectionId} subscribed to topic: {Topic}",
            Context.ConnectionId, topic);
    }

    /// <summary>
    /// Called by clients to unsubscribe from a topic.
    /// </summary>
    /// <param name="topic">The group/topic name to unsubscribe from.</param>
    public async Task UnsubscribeFromTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new HubException("Topic name cannot be empty.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, topic);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from topic: {Topic}",
            Context.ConnectionId, topic);
    }

    /// <summary>
    /// Called by a client to send a notification to all subscribers of a topic.
    /// The server relays this to all group members.
    /// </summary>
    /// <param name="topic">The target group/topic.</param>
    /// <param name="title">Notification title.</param>
    /// <param name="body">Notification body text.</param>
    public async Task SendToTopic(string topic, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new HubException("Topic name cannot be empty.");
        }

        var notification = new NotificationMessage
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Body = body,
            Topic = topic,
            SentAt = DateTime.UtcNow,
            SenderId = Context.ConnectionId
        };

        // Broadcast to all subscribers of the topic
        await Clients.Group(topic).ReceiveNotification(notification);

        // Acknowledge delivery back to the sender
        await Clients.Caller.ReceiveAcknowledgement(notification.Id);

        _logger.LogInformation(
            "Notification {NotificationId} sent to topic '{Topic}' by {SenderId}",
            notification.Id, topic, Context.ConnectionId);
    }
}

/// <summary>
/// Represents a real-time notification message sent between clients via the hub.
/// </summary>
public sealed record NotificationMessage
{
    /// <summary>Unique notification identifier (GUID string).</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Short descriptive title of the notification.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Full notification body or payload.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>The topic/channel this notification was sent to.</summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>UTC timestamp of when the notification was sent.</summary>
    public DateTime SentAt { get; init; } = DateTime.UtcNow;

    /// <summary>SignalR connection ID of the sender (server-assigned).</summary>
    public string? SenderId { get; init; }

    /// <summary>Optional severity level: info | warning | error | success.</summary>
    public string Severity { get; init; } = "info";
}
