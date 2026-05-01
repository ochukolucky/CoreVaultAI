namespace CoreVault.Contracts.Events.Transactions;

/// <summary>
/// Published by: CoreVault.Transactions
/// Consumed by:  CoreVault.Audit
///               CoreVault.Notifications
///               CoreVault.OpenBanking (webhook delivery to fintechs)
///
/// Published AFTER AI approval. This is the confirmation
/// that money has actually moved and ledger entries are written.
/// </summary>
public sealed record TransactionCompletedEvent : BaseEvent
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public Guid CustomerId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Currency { get; init; } = "MYR";
    public string Reference { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}