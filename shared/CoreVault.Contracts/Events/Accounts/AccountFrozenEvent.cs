namespace CoreVault.Contracts.Events.Accounts;

/// <summary>
/// Published by: CoreVault.Accounts
/// Consumed by:  CoreVault.Transactions ← blocks new transactions
///               CoreVault.Notifications, CoreVault.Audit
///
/// An account is frozen when fraud is confirmed or
/// compliance issues a freeze order.
/// </summary>
public sealed record AccountFrozenEvent : BaseEvent
{
    public Guid AccountId { get; init; }
    public Guid CustomerId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string FrozenBy { get; init; } = string.Empty; // staff ID or "FraudAgent"
    public DateTime FrozenAt { get; init; }
}