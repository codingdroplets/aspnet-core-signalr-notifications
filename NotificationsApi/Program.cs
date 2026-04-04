using NotificationsApi.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

// Add MVC controllers
builder.Services.AddControllers();

// Add SignalR — this registers all SignalR services required for hub routing
builder.Services.AddSignalR(options =>
{
    // Maximum time the server waits before considering a client disconnected.
    // After this interval, OnDisconnectedAsync is called.
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

    // Interval at which the server sends a keep-alive ping to all connected clients.
    // Must be less than ClientTimeoutInterval.
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);

    // Enable detailed error messages in development.
    // In production, keep this false to prevent leaking internal details.
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Add OpenAPI / Swagger for REST endpoint documentation
builder.Services.AddOpenApi();

// Add CORS — required if the SignalR client runs in a browser from a different origin
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:5289", "https://localhost:7163")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR WebSocket connections
    });
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    // Serve the OpenAPI spec at /openapi/v1.json
    // Access it at: http://localhost:5289/openapi/v1.json
    app.MapOpenApi();

    // Use built-in Scalar UI (available in .NET 9+/10) for interactive API docs
    // Access at: http://localhost:5289/scalar/v1
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Notifications API");
    });
}

// Apply CORS before routing
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────

// Map REST API controllers
app.MapControllers();

// Map the SignalR hub at its dedicated endpoint.
// Clients connect here using the SignalR client library.
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
