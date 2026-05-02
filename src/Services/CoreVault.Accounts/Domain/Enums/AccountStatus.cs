namespace CoreVault.Accounts.Domain.Enums;

/// <summary>
/// The lifecycle status of a bank account.
///
/// PendingActivation → account created, not yet active
///                     waiting for initial deposit
/// Active            → fully operational, all transactions allowed
/// Dormant           → no activity for 12 months, limited transactions
///                     BNM regulation — banks must flag inactive accounts
/// Frozen            → blocked by compliance or fraud detection
///                     no transactions in or out
/// Closed            → permanently closed, no transactions allowed
///                     record retained for 7 years (BNM requirement)
/// </summary>
public enum AccountStatus
{
    PendingActivation = 0,
    Active = 1,
    Dormant = 2,
    Frozen = 3,
    Closed = 4
}