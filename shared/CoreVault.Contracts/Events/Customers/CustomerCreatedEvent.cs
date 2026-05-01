namespace CoreVault.Contracts.Events.Customers;

/// <summary>
/// Published by: CoreVault.Customer
/// Consumed by:  CoreVault.Audit, CoreVault.Notifications
/// </summary>
public sealed record CustomerCreatedEvent : BaseEvent
{
    public Guid CustomerId { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Nationality { get; init; } = string.Empty;
}