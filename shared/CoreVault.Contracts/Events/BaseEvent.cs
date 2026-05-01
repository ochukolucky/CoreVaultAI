namespace CoreVault.Contracts.Events;

/// <summary>
/// Every event published to RabbitMQ inherits from this.
/// EventId      → unique ID for idempotency (consumers check this 
///                before processing to avoid duplicate handling)
/// OccurredAt   → when it happened on the publishing service
/// CorrelationId → ties together all events from one user action.
///                 Example: one Transfer triggers TransactionInitiated,
///                 FraudDetected, TransactionCompleted, Notification —
///                 all share the same CorrelationId for tracing.
/// </summary>
public abstract record BaseEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; }
    public string EventType => GetType().Name;
}