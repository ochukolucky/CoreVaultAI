namespace CoreVault.Contracts.Events.Accounts;

/// <summary>
/// Published by: CoreVault.Accounts
/// Consumed by:  CoreVault.Notifications, CoreVault.Audit,
///               CoreVault.OpenBanking
/// </summary>
public sealed record AccountOpenedEvent : BaseEvent
{
    public Guid AccountId { get; init; }
    public Guid CustomerId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty; // Savings / Current / FixedDeposit
    public decimal InitialBalance { get; init; }
    public string Currency { get; init; } = "MYR";
    public decimal DailyTransactionLimit { get; init; } // ← add this
    public DateTime OpenedAt { get; init; }
}