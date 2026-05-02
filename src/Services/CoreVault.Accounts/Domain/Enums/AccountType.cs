namespace CoreVault.Accounts.Domain.Enums;

/// <summary>
/// The type of bank account.
///
/// Savings      → earns interest, limited withdrawals per month
/// Current      → no interest, unlimited transactions, for businesses
/// FixedDeposit → locked for a term, higher interest rate
/// </summary>
public enum AccountType
{
    Savings = 1,
    Current = 2,
    FixedDeposit = 3
}