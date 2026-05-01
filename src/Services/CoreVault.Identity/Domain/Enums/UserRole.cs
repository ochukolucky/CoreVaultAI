namespace CoreVault.Identity.Domain.Enums;

/// <summary>
/// Defines every role in the CoreVault AI system.
/// Roles control what a user can see and do across all services.
/// 
/// Customer      → end user, own accounts only
/// Teller        → branch staff, assist customers
/// BackOffice    → operations team, review flagged transactions
/// Compliance    → regulatory team, full audit access
/// Admin         → IT team, system configuration
/// Fintech       → external third-party API consumer
/// </summary>
public enum UserRole
{
    Customer = 1,
    Teller = 2,
    BackOffice = 3,
    Compliance = 4,
    Admin = 5,
    Fintech = 6
}