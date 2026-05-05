namespace CoreVault.Transactions.Domain.Enums;

/// <summary>
/// The type of financial transaction.
///
/// Deposit     → money coming INTO an account from external source
/// Withdrawal  → money going OUT of an account to external destination
/// Transfer    → money moving between two accounts within CoreVault
/// Reversal    → corrects a previously completed transaction
///               always references the original TransactionId
/// </summary>
public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Reversal = 4
}