namespace CoreVault.Contracts.Events.AI;

/// <summary>
/// Published by: CoreVault.AIIntelligence
/// Consumed by:  CoreVault.Transactions
///
/// Green light from the AI layer.
/// Transaction service receives this and proceeds
/// to write ledger entries and complete the transaction.
/// </summary>
public sealed record TransactionApprovedEvent : BaseEvent
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public int RiskScore { get; init; }       // 0-100
    public double Confidence { get; init; }   // 0.0-1.0
    public bool FallbackUsed { get; init; }   // true = rule engine, not Claude
    public DateTime ApprovedAt { get; init; }
}