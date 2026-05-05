using CoreVault.SharedKernel.Entities;
using CoreVault.SharedKernel.Guards;

namespace CoreVault.Transactions.Domain.Entities;

/// <summary>
/// Local projection of account details.
/// Built from AccountOpenedEvent consumed from RabbitMQ.
///
/// Transaction Service never calls Accounts Service directly.
/// It maintains its own local copy of account information
/// needed to validate and process transactions.
///
/// Used for:
/// 1. Ownership validation — does this AccountId belong
///    to this CustomerId?
/// 2. Account number resolution — what is the display
///    account number for this AccountId?
/// 3. Status check — is this account active and can transact?
/// </summary>
public sealed class AccountSummary : BaseEntity
{
    public Guid AccountId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string AccountType { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "MYR";
    public decimal DailyTransactionLimit { get; private set; }

    // Required by EF Core
    private AccountSummary() { }

    private AccountSummary(Guid id) : base(id) { }

    public static AccountSummary Create(
        Guid accountId,
        Guid customerId,
        string accountNumber,
        string accountType,
        string status,
        string currency,
        decimal dailyTransactionLimit)
    {
        Guard.AgainstEmptyGuid(accountId, nameof(accountId));
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Guard.AgainstNullOrEmpty(accountNumber, nameof(accountNumber));
        Guard.AgainstNullOrEmpty(accountType, nameof(accountType));
        Guard.AgainstNullOrEmpty(status, nameof(status));

        return new AccountSummary(Guid.NewGuid())
        {
            AccountId = accountId,
            CustomerId = customerId,
            AccountNumber = accountNumber,
            AccountType = accountType,
            Status = status,
            Currency = currency,
            DailyTransactionLimit = dailyTransactionLimit
        };
    }

    /// <summary>
    /// Called when AccountFrozenEvent is consumed.
    /// Blocks transactions from this account immediately.
    /// </summary>
    public void MarkFrozen() =>
        Status = "Frozen";

    /// <summary>
    /// Called when account is unfrozen.
    /// </summary>
    public void MarkActive() =>
        Status = "Active";

    public bool CanTransact =>
        Status == "Active";
}