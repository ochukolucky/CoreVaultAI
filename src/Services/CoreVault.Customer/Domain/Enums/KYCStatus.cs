namespace CoreVault.Customer.Domain.Enums;

/// <summary>
/// Tracks where a customer is in the KYC lifecycle.
/// 
/// NotStarted  → profile created, no documents submitted yet
/// Pending     → documents submitted, AI agent reviewing
/// Verified    → AI approved, customer can open accounts
/// Rejected    → AI rejected, customer must resubmit
/// Expired     → KYC approved but documents past validity period
///               customer must re-verify
/// Suspended   → compliance team manually suspended
/// </summary>
public enum KYCStatus
{
    NotStarted = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3,
    Expired = 4,
    Suspended = 5
}