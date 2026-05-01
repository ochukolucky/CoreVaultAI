namespace CoreVault.Contracts.Events.Transactions;

/// <summary>
/// Published by: CoreVault.Transactions
/// Consumed by:  CoreVault.AIIntelligence ← ONLY consumer
///
/// This is the most performance-critical event in the system.
/// Every single transaction publishes this event.
/// The AI Intelligence service scores it and publishes
/// either TransactionApprovedEvent or FraudDetectedEvent.
///
/// DeviceFingerprint and IpAddress feed the AI fraud model —
/// unusual device or location is a key fraud signal.
/// </summary>
public sealed record TransactionInitiatedEvent : BaseEvent
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public Guid CustomerId { get; init; }
    public string TransactionType { get; init; } = string.Empty; // Deposit/Withdraw/Transfer
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "MYR";
    public Guid? DestinationAccountId { get; init; } // null for non-transfers
    public string? DeviceFingerprint { get; init; }
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public DateTime InitiatedAt { get; init; }
}