namespace CoreVault.Contracts.Events.Customers;

/// <summary>
/// Published by: CoreVault.AgenticWorkflow
/// Consumed by:  CoreVault.Notifications, CoreVault.Audit
/// </summary>
public sealed record KYCRejectedEvent : BaseEvent
{
    public Guid CustomerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string RejectionReason { get; init; } = string.Empty;
    public double AIConfidenceScore { get; init; }
    public string RejectedBy { get; init; } = string.Empty;
    public DateTime RejectedAt { get; init; }
}