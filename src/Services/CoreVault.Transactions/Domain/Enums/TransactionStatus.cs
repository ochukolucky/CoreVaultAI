namespace CoreVault.Transactions.Domain.Enums;

/// <summary>
/// The lifecycle status of a transaction.
///
/// Pending   → created, waiting for AI fraud scoring
///             money has NOT moved yet
///             this is the initial state of every transaction
///
/// Approved  → AI scoring complete, transaction cleared
///             ready for settlement
///             money is about to move
///
/// Completed → money has moved, ledger entries written
///             this is the terminal success state
///             IMMUTABLE — never changes after this point
///
/// Failed    → validation failed before AI scoring
///             e.g. insufficient funds, account frozen
///             money never left the account
///
/// Blocked   → AI detected fraud, transaction stopped
///             money did not move
///             compliance team is notified
///
/// Reversed  → a Completed transaction that was later reversed
///             original transaction still exists — immutable
///             a new Reversal transaction is created to correct it
/// </summary>
public enum TransactionStatus
{
    Pending = 0,
    Approved = 1,
    Completed = 2,
    Failed = 3,
    Blocked = 4,
    Reversed = 5
}