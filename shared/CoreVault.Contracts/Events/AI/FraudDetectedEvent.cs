namespace CoreVault.Contracts.Events.AI;

/// <summary>
/// Published by: CoreVault.AIIntelligence
/// Consumed by:  CoreVault.Transactions ← blocks the transaction
///               CoreVault.Accounts     ← may freeze the account
///               CoreVault.Audit        ← mandatory compliance record
///               CoreVault.Notifications ← alert customer and compliance team
///
/// This is the highest priority event in the system.
/// All four consumers act on it simultaneously via fanout.
/// </summary>
public sealed record FraudDetectedEvent : BaseEvent
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public Guid CustomerId { get; init; }
    public int RiskScore { get; init; }
    public double Confidence { get; init; }
    public List<string> FraudSignals { get; init; } = [];
    public string Explanation { get; init; } = string.Empty;
    public bool FallbackUsed { get; init; }
    public string Recommendation { get; init; } = string.Empty; // Hold / Block
    public DateTime DetectedAt { get; init; }
}