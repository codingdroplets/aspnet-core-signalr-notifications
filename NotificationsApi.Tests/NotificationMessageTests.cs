using NotificationsApi.Hubs;
using Xunit;

namespace NotificationsApi.Tests;

/// <summary>
/// Unit tests for the NotificationMessage record.
/// Validates default values, immutability, and record equality semantics.
/// </summary>
public class NotificationMessageTests
{
    [Fact]
    public void NotificationMessage_DefaultId_IsValidGuid()
    {
        // Act
        var msg = new NotificationMessage();

        // Assert
        Assert.True(Guid.TryParse(msg.Id, out _),
            "Default Id should be a valid GUID string.");
    }

    [Fact]
    public void NotificationMessage_DefaultSeverity_IsInfo()
    {
        // Act
        var msg = new NotificationMessage();

        // Assert
        Assert.Equal("info", msg.Severity);
    }

    [Fact]
    public void NotificationMessage_SentAt_IsCloseToUtcNow()
    {
        // Act
        var before = DateTime.UtcNow.AddSeconds(-1);
        var msg = new NotificationMessage();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert — SentAt should be between before and after
        Assert.InRange(msg.SentAt, before, after);
    }

    [Fact]
    public void NotificationMessage_CanSetAllProperties()
    {
        // Arrange & Act
        var fixedTime = new DateTime(2026, 4, 4, 6, 0, 0, DateTimeKind.Utc);
        var msg = new NotificationMessage
        {
            Id = "test-id-123",
            Title = "Test Title",
            Body = "Test Body",
            Topic = "orders",
            SentAt = fixedTime,
            SenderId = "conn-abc-def",
            Severity = "warning"
        };

        // Assert
        Assert.Equal("test-id-123", msg.Id);
        Assert.Equal("Test Title", msg.Title);
        Assert.Equal("Test Body", msg.Body);
        Assert.Equal("orders", msg.Topic);
        Assert.Equal(fixedTime, msg.SentAt);
        Assert.Equal("conn-abc-def", msg.SenderId);
        Assert.Equal("warning", msg.Severity);
    }

    [Fact]
    public void NotificationMessage_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var fixedTime = new DateTime(2026, 4, 4, 6, 0, 0, DateTimeKind.Utc);
        var msg1 = new NotificationMessage
        {
            Id = "same-id",
            Title = "Same",
            Body = "Same body",
            Topic = "alerts",
            SentAt = fixedTime,
            Severity = "error"
        };

        var msg2 = msg1 with { }; // record copy

        // Assert — records with same values are equal
        Assert.Equal(msg1, msg2);
    }

    [Fact]
    public void NotificationMessage_WithModifiedProperty_AreNotEqual()
    {
        // Arrange
        var original = new NotificationMessage
        {
            Id = "id-1",
            Title = "Original",
            Body = "Body",
            Topic = "test"
        };

        var modified = original with { Severity = "error" };

        // Assert
        Assert.NotEqual(original, modified);
    }
}
