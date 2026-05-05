namespace CoreVault.Transactions.Domain.Entities;

/// <summary>
/// Represents an event that needs to be published to RabbitMQ.
///
/// The Outbox Pattern works like this:
/// 1. Transaction record saved
/// 2. OutboxMessage saved
/// Both in ONE database transaction — atomic.
///
/// A background job then reads unpublished OutboxMessages
/// and publishes them to RabbitMQ.
/// If publishing fails → retry up to 3 times
/// After 3 retries → marked as failed → ops team alerted
///
/// This guarantees:
/// - No event is ever lost even if app crashes
/// - No duplicate processing (ProcessedAt tracks completion)
/// - Full audit trail of every event ever published
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Required by EF Core
    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Called by the background publisher when
    /// the event is successfully published to RabbitMQ.
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called when publishing fails.
    /// After MaxRetries the message stays unprocessed
    /// and the monitoring system alerts the ops team.
    /// </summary>
    public void RecordFailure(string errorMessage)
    {
        RetryCount++;
        ErrorMessage = errorMessage;
    }

    public bool IsProcessed => ProcessedAt.HasValue;
    public bool HasExceededRetryLimit => RetryCount >= 3;
}