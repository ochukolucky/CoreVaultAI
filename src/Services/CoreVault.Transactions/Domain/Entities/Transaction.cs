using CoreVault.SharedKernel.Entities;
using CoreVault.SharedKernel.Guards;
using CoreVault.Transactions.Domain.Enums;

namespace CoreVault.Transactions.Domain.Entities;

/// <summary>
/// The Transaction aggregate root.
/// Represents a single financial movement in the system.
///
/// IMMUTABILITY RULE:
/// Once a transaction reaches Completed or Blocked status
/// it can never be modified. Corrections are made by creating
/// a new Reversal transaction — never by editing the original.
/// This guarantees a complete and tamper-proof audit trail.
///
/// OUTBOX RULE:
/// Every state change that needs to be communicated to other
/// services is recorded in the OutboxMessages table in the
/// same database transaction as the status change itself.
/// </summary>
public sealed class Transaction : AggregateRoot
{
    public Guid AccountId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? DestinationAccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "MYR";
    public string Reference { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? DeviceFingerprint { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Location { get; private set; }
    public string? FailureReason { get; private set; }
    public string? FraudReason { get; private set; }
    public Guid? OriginalTransactionId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int RiskScore { get; private set; }

    // Required by EF Core
    private Transaction() { }

    private Transaction(Guid id) : base(id) { }

    /// <summary>
    /// Creates a new transaction in Pending status.
    /// Money has NOT moved at this point.
    /// The transaction exists only to track the intent
    /// until AI scoring completes.
    /// </summary>
    public static Transaction Create(
        Guid accountId,
        Guid customerId,
        TransactionType type,
        decimal amount,
        string idempotencyKey,
        string reference,
        Guid? destinationAccountId = null,
        string? deviceFingerprint = null,
        string? ipAddress = null,
        string? location = null)
    {
        Guard.AgainstEmptyGuid(accountId, nameof(accountId));
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Guard.AgainstNegativeOrZero(amount, nameof(amount));
        Guard.AgainstNullOrEmpty(idempotencyKey, nameof(idempotencyKey));
        Guard.AgainstNullOrEmpty(reference, nameof(reference));

        return new Transaction(Guid.NewGuid())
        {
            AccountId = accountId,
            CustomerId = customerId,
            DestinationAccountId = destinationAccountId,
            Type = type,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Currency = "MYR",
            Reference = reference,
            IdempotencyKey = idempotencyKey,
            DeviceFingerprint = deviceFingerprint,
            IpAddress = ipAddress,
            Location = location
        };
    }

    // ── State Transitions ────────────────────────────────────────
    // Each method enforces valid state transitions.
    // You cannot complete a transaction that was never pending.
    // You cannot block a transaction that already completed.
    // Invalid transitions throw — fail fast, fail loud.

    /// <summary>
    /// Called when AI Intelligence publishes TransactionApprovedEvent.
    /// Transaction is cleared for settlement.
    /// </summary>
    public void Approve(int riskScore)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot approve transaction in {Status} status.");

        Status = TransactionStatus.Approved;
        RiskScore = riskScore;
    }

    /// <summary>
    /// Called after ledger entries are written.
    /// Money has moved. This is the terminal success state.
    /// IMMUTABLE after this point.
    /// </summary>
    public void Complete()
    {
        if (Status != TransactionStatus.Approved)
            throw new InvalidOperationException($"Cannot complete transaction in {Status} status.");

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called when validation fails before AI scoring.
    /// e.g. insufficient funds, frozen account.
    /// Money never moved.
    /// </summary>
    public void Fail(string reason)
    {
        Guard.AgainstNullOrEmpty(reason, nameof(reason));

        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot fail transaction in {Status} status.");

        Status = TransactionStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>
    /// Called when AI Intelligence publishes FraudDetectedEvent.
    /// Money did not move. Compliance team is notified.
    /// </summary>
    public void Block(string fraudReason)
    {
        Guard.AgainstNullOrEmpty(fraudReason, nameof(fraudReason));

        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot block transaction in {Status} status.");

        Status = TransactionStatus.Blocked;
        FraudReason = fraudReason;
    }

    /// <summary>
    /// Creates a new Reversal transaction linked to this one.
    /// The original transaction is NOT modified — ever.
    /// A new transaction record is created to represent the reversal.
    /// </summary>
    public static Transaction CreateReversal(Transaction original, string idempotencyKey)
    {
        if (original.Status != TransactionStatus.Completed)
           throw new InvalidOperationException("Only completed transactions can be reversed.");

        return new Transaction(Guid.NewGuid())
        {
            AccountId = original.AccountId,
            CustomerId = original.CustomerId,
            DestinationAccountId = original.DestinationAccountId,
            Type = TransactionType.Reversal,
            Status = TransactionStatus.Pending,
            Amount = original.Amount,
            Currency = original.Currency,
            Reference = $"REVERSAL-{original.Reference}",
            IdempotencyKey = idempotencyKey,
            OriginalTransactionId = original.Id
        };
    }
}