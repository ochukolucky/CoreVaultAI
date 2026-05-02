namespace CoreVault.Contracts.Events.Identity;

/// <summary>
/// Published by: CoreVault.Identity
/// Consumed by:  CoreVault.Customer
///
/// When a user registers an account, the Customer service
/// listens to this event and creates a matching customer profile.
/// This is how Identity and Customer stay decoupled —
/// Identity never calls Customer directly.
/// </summary>
public sealed record UserRegisteredEvent : BaseEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}