namespace CoreVault.Contracts.Events.Customers;

/// <summary>
/// Published by: CoreVault.Customer
/// Consumed by:  CoreVault.AgenticWorkflow
///
/// This triggers the KYC Verification Agent workflow.
/// The agent picks this up, gathers document context,
/// calls Claude API, and publishes KYCApproved or KYCRejected.
/// </summary>
public sealed record KYCSubmittedEvent : BaseEvent
{
    public Guid CustomerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public List<string> DocumentUrls { get; init; } = [];
}