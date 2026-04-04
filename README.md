# aspnet-core-signalr-notifications

> Real-Time Notifications with SignalR in ASP.NET Core — typed hub, topic subscriptions, server-push broadcasts, and a clean REST trigger API.

[![Visit CodingDroplets](https://img.shields.io/badge/Website-codingdroplets.com-blue?style=for-the-badge&logo=google-chrome&logoColor=white)](https://codingdroplets.com/)
[![YouTube](https://img.shields.io/badge/YouTube-CodingDroplets-red?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/@CodingDroplets)
[![Patreon](https://img.shields.io/badge/Patreon-Support%20Us-orange?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/CodingDroplets)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-Support%20Us-yellow?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/codingdroplets)
[![GitHub](https://img.shields.io/badge/GitHub-codingdroplets-black?style=for-the-badge&logo=github&logoColor=white)](http://github.com/codingdroplets/)

---

## 🚀 Support the Channel — Join on Patreon

If this sample saved you time, consider joining our Patreon community.
You'll get **exclusive .NET tutorials, premium code samples, and early access** to new content — all for the price of a coffee.

👉 **[Join CodingDroplets on Patreon](https://www.patreon.com/CodingDroplets)**

Prefer a one-time tip? [Buy us a coffee ☕](https://buymeacoffee.com/codingdroplets)

---

## 🎯 What You'll Learn

- How to set up a **SignalR hub** in ASP.NET Core with typed client interfaces
- How to implement **topic-based (group) subscriptions** so clients only receive relevant notifications
- How to **push notifications from a REST controller** using `IHubContext<THub, TClient>` — ideal for background jobs, webhooks, and event handlers
- How to configure **keep-alive intervals and client timeout** settings in SignalR
- How to configure **CORS correctly for SignalR** WebSocket connections (`AllowCredentials` + explicit origins)
- How to write **integration tests** for SignalR-backed REST endpoints using `WebApplicationFactory`
- How to use the **Scalar API UI** (the modern OpenAPI explorer that ships with .NET 10)

---

## 🗺️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Browser / App                     │
│                                                                 │
│  ┌─────────────────┐         ┌──────────────────────────────┐  │
│  │  SignalR Client │         │   HTTP Client (fetch/axios)  │  │
│  │  (WebSocket /   │         │                              │  │
│  │  Long-Polling)  │         └──────────────┬───────────────┘  │
└──┼─────────────────┼───────────────────────┼──────────────────┘
   │                 │                       │
   │  WS/SSE         │                       │ REST POST
   ▼                 ▼                       ▼
┌──────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Application                      │
│                                                                  │
│  ┌───────────────────────────┐  ┌──────────────────────────────┐ │
│  │      NotificationHub      │  │   NotificationsController    │ │
│  │   /hubs/notifications     │  │   /api/notifications/...     │ │
│  │                           │  │                              │ │
│  │  OnConnectedAsync()       │  │  POST /broadcast             │ │
│  │    → join "all-clients"   │  │  POST /topic/{topic}         │ │
│  │                           │  │  GET  /status                │ │
│  │  SubscribeToTopic(topic)  │  │                              │ │
│  │    → join group           │  │  Uses IHubContext<Hub,Client>│ │
│  │                           │  │  to push from REST → Hub     │ │
│  │  SendToTopic(topic, ...)  │  └──────────────────────────────┘ │
│  │    → Clients.Group(topic) │                                   │
│  │       .ReceiveNotif(msg)  │  ← Typed client interface:        │
│  │    → Caller.Acknowledge() │    INotificationClient           │
│  └───────────────────────────┘                                   │
│                                                                  │
│  SignalR Groups (topic channels):                                │
│    "all-clients"  → every connected client                       │
│    "orders"       → clients subscribed to order events           │
│    "alerts"       → clients subscribed to system alerts          │
│    "payments"     → clients subscribed to payment events         │
│    (any custom topic name)                                       │
└──────────────────────────────────────────────────────────────────┘
```

---

## 📋 Summary Table

| Concept | Description | Where in Code |
|---------|-------------|---------------|
| **Typed Hub** | `Hub<INotificationClient>` enforces a contract for server→client calls | `NotificationHub.cs` |
| **Hub Groups** | Topic-based channels; clients opt-in via `SubscribeToTopic()` | `NotificationHub.cs` |
| **Server Push** | REST endpoints push via `IHubContext<Hub, Client>` | `NotificationsController.cs` |
| **Client-to-Group** | `SendToTopic()` lets a client broadcast to a topic | `NotificationHub.cs` |
| **Broadcast** | `Clients.All` sends to every connected client | `NotificationsController.cs` |
| **Keep-Alive** | 15s ping interval, 60s disconnect timeout | `Program.cs` |
| **CORS** | `AllowCredentials()` required for WebSocket origin check | `Program.cs` |

---

## 📁 Project Structure

```
aspnet-core-signalr-notifications/
├── aspnet-core-signalr-notifications.sln
│
├── NotificationsApi/
│   ├── Controllers/
│   │   └── NotificationsController.cs   # REST endpoints: broadcast + topic push
│   ├── Hubs/
│   │   └── NotificationHub.cs           # SignalR hub + typed client interface + message model
│   ├── Properties/
│   │   └── launchSettings.json          # Dev launch config (opens API UI on start)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                       # DI setup, CORS, SignalR, middleware pipeline
│
└── NotificationsApi.Tests/
    ├── NotificationsControllerTests.cs  # Integration tests for REST endpoints (10 tests)
    └── NotificationMessageTests.cs      # Unit tests for the message model (6 tests)
```

---

## 🛠️ Prerequisites

| Requirement | Version |
|-------------|---------|
| .NET SDK | 10.0+ |
| IDE | Visual Studio 2022 / Rider / VS Code |
| Browser | Any modern browser (for Scalar API UI) |

---

## ⚡ Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/codingdroplets/aspnet-core-signalr-notifications.git
cd aspnet-core-signalr-notifications

# 2. Build the solution
dotnet build -c Release

# 3. Run the API
cd NotificationsApi
dotnet run

# 4. Open the interactive API UI
# http://localhost:5289/scalar/v1
```

The app opens **Scalar API UI** automatically when running from Visual Studio.

---

## 🔧 How It Works

### 1. SignalR Hub Setup

The `NotificationHub` is a typed hub — it implements `Hub<INotificationClient>` where `INotificationClient` defines the methods the server can call on clients:

```csharp
public interface INotificationClient
{
    Task ReceiveNotification(NotificationMessage message);
    Task ReceiveAcknowledgement(string notificationId);
}

public class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        // Every client automatically joins the "all-clients" broadcast group
        await Groups.AddToGroupAsync(Context.ConnectionId, "all-clients");
        await base.OnConnectedAsync();
    }
}
```

### 2. Topic Subscriptions (Groups)

Clients subscribe to named topics (SignalR Groups) to receive targeted notifications:

```csharp
// Client calls this hub method to subscribe
public async Task SubscribeToTopic(string topic)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, topic);
}

// Client calls this to unsubscribe
public async Task UnsubscribeFromTopic(string topic)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, topic);
}
```

### 3. Client-to-Topic Messaging

A connected client can broadcast to other subscribers of a topic:

```csharp
public async Task SendToTopic(string topic, string title, string body)
{
    var notification = new NotificationMessage { Title = title, Body = body, Topic = topic, ... };

    await Clients.Group(topic).ReceiveNotification(notification); // push to group
    await Clients.Caller.ReceiveAcknowledgement(notification.Id); // ack to sender
}
```

### 4. Server-Push via IHubContext

Your backend (REST controllers, background jobs, event handlers) can push without a connected client:

```csharp
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest request)
    {
        var notification = new NotificationMessage { ... };
        await _hubContext.Clients.All.ReceiveNotification(notification);
        return Ok(...);
    }

    [HttpPost("topic/{topic}")]
    public async Task<IActionResult> SendToTopic(string topic, [FromBody] BroadcastRequest request)
    {
        var notification = new NotificationMessage { Topic = topic, ... };
        await _hubContext.Clients.Group(topic).ReceiveNotification(notification);
        return Ok(...);
    }
}
```

### 5. SignalR Configuration

```csharp
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);  // disconnect after 60s silence
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);      // ping every 15s
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
```

### 6. CORS for WebSocket Connections

SignalR WebSocket connections require `AllowCredentials()` — use explicit origins (not wildcard):

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5289")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR!
    });
});
```

### 7. Connecting from a JavaScript Client

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5289/hubs/notifications")
    .withAutomaticReconnect()        // auto-reconnect with backoff
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Listen for incoming notifications
connection.on("ReceiveNotification", (message) => {
    console.log(`[${message.topic}] ${message.title}: ${message.body}`);
});

// Listen for acknowledgements
connection.on("ReceiveAcknowledgement", (notificationId) => {
    console.log(`Message ${notificationId} acknowledged.`);
});

// Start and subscribe to a topic
await connection.start();
await connection.invoke("SubscribeToTopic", "orders");
```

---

## 📡 API Endpoints

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| `GET` | `/api/notifications/status` | Health check — returns service status | `200 OK` |
| `POST` | `/api/notifications/broadcast` | Push a notification to **all** connected clients | `200 OK` |
| `POST` | `/api/notifications/topic/{topic}` | Push to clients subscribed to `{topic}` | `200 OK` |
| `WS` | `/hubs/notifications` | SignalR WebSocket hub endpoint | — |

### Hub Methods (client → server)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `SubscribeToTopic` | `topic: string` | Subscribe to a notification topic (join group) |
| `UnsubscribeFromTopic` | `topic: string` | Unsubscribe from a topic (leave group) |
| `SendToTopic` | `topic, title, body` | Broadcast a notification to a topic from client |

### Client Methods (server → client)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `ReceiveNotification` | `NotificationMessage` | Delivers a notification to the client |
| `ReceiveAcknowledgement` | `notificationId: string` | Confirms delivery of a sent message |

---

## 🧪 Running Tests

```bash
dotnet test -c Release
```

```
Test Run Successful.
Total tests: 16
     Passed: 16
```

| Test Class | Tests | What's Covered |
|------------|-------|----------------|
| `NotificationsControllerTests` | 10 | Broadcast, topic routing, validation, unique IDs |
| `NotificationMessageTests` | 6 | Default values, properties, record equality |

---

## 🤔 Key Concepts

### Why Typed Hubs?

| Approach | Pros | Cons |
|----------|------|------|
| `Hub` (untyped) | Simple, quick | Typos at runtime, no IntelliSense for client methods |
| `Hub<T>` (typed) | Compile-time safety, IntelliSense, refactor-safe | Slightly more setup |

Typed hubs (`Hub<INotificationClient>`) are the recommended approach for any non-trivial application.

### Why `IHubContext` for Server Push?

`IHubContext` is the mechanism for pushing from **outside** a hub — from controllers, background services, Hangfire jobs, event handlers, etc. Without it, you'd need a connected client to trigger a message, which defeats the purpose of server-side push.

### Topic Groups vs Connection IDs

| Strategy | Use Case |
|----------|----------|
| `Clients.All` | System-wide announcements |
| `Clients.Group(topic)` | Opt-in topic channels (orders, alerts, payments) |
| `Clients.Client(connectionId)` | Direct message to a specific connection |
| `Clients.User(userId)` | All connections for a specific authenticated user |

### Transport Fallback Order

SignalR automatically selects the best transport:
1. **WebSockets** (preferred, lowest latency)
2. **Server-Sent Events** (SSE)
3. **Long Polling** (fallback for restricted environments)

---

## 🏷️ Technologies Used

- **.NET 10** — Runtime platform
- **ASP.NET Core** — Web framework
- **SignalR** — Real-time communication layer
- **Scalar.AspNetCore** — Modern OpenAPI UI (replaces Swagger UI in .NET 10)
- **Microsoft.AspNetCore.OpenApi** — OpenAPI spec generation
- **xUnit** — Test framework
- **Microsoft.AspNetCore.Mvc.Testing** — Integration test infrastructure

---

## 📚 References

- [ASP.NET Core SignalR — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [SignalR Groups — Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/groups)
- [IHubContext — Use SignalR outside a hub](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext)

---

## 📄 License

This project is licensed under the **MIT License** — free to use in personal and commercial projects.

---

## 🔗 Connect with CodingDroplets

| Platform | Link |
|----------|------|
| 🌐 Website | https://codingdroplets.com/ |
| 📺 YouTube | https://www.youtube.com/@CodingDroplets |
| 🎁 Patreon | https://www.patreon.com/CodingDroplets |
| ☕ Buy Me a Coffee | https://buymeacoffee.com/codingdroplets |
| 💻 GitHub | http://github.com/codingdroplets/ |

> **Want more samples like this?** [Support us on Patreon](https://www.patreon.com/CodingDroplets) or [buy us a coffee ☕](https://buymeacoffee.com/codingdroplets) — every bit helps keep the content coming!
