namespace CoreVault.Contracts.Events.Customers;

/// <summary>
/// Published by: CoreVault.AgenticWorkflow (after AI agent decision)
/// Consumed by:  CoreVault.Accounts  ← unlocks account opening
///               CoreVault.Notifications ← tells customer they're approved
///               CoreVault.Audit     ← records the decision
///
/// Note: RiskTier is set by the AI agent based on document
/// analysis and sanctions screening.
/// Low / Medium / High
/// </summary>
public sealed record KYCApprovedEvent : BaseEvent
{
    public Guid CustomerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string RiskTier { get; init; } = string.Empty;
    public double AIConfidenceScore { get; init; }
    public string ApprovedBy { get; init; } = string.Empty; // "KYCAgent" or staff ID
    public DateTime ApprovedAt { get; init; }
}