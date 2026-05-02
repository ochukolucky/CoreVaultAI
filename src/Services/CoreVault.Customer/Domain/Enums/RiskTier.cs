namespace CoreVault.Customer.Domain.Enums;

/// <summary>
/// Set by the AI KYC agent after document verification.
/// Controls transaction limits and monitoring frequency.
///
/// Low     → standard customer, normal limits
/// Medium  → enhanced monitoring, slightly lower limits
/// High    → strict monitoring, reduced limits, 
///           manual approval for large transactions
/// </summary>
public enum RiskTier
{
    NotAssigned = 0,
    Low = 1,
    Medium = 2,
    High = 3
}