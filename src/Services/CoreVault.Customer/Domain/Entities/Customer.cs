using CoreVault.Customer.Domain.Enums;
using CoreVault.SharedKernel.Entities;
using CoreVault.SharedKernel.Guards;

namespace CoreVault.Customer.Domain.Entities;

/// <summary>
/// The Customer aggregate root.
/// 
/// Important distinction from Identity:
///   Identity → ApplicationUser = WHO can log in
///   Customer → Customer = WHO has a banking relationship
///
/// A user becomes a customer when their profile is created.
/// A customer can open accounts only after KYC is verified.
/// These are deliberately separate concerns in separate services.
/// </summary>
public sealed class Customer : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Nationality { get; private set; }
    public string? Address { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public KYCStatus KYCStatus { get; private set; }
    public RiskTier RiskTier { get; private set; }
    public DateTime? KYCVerifiedAt { get; private set; }
    public DateTime? KYCExpiresAt { get; private set; }
    public string? KYCRejectionReason { get; private set; }
    public bool IsActive { get; private set; }

    // Private constructor — passes Id up to BaseEntity
    private Customer(Guid id) : base(id) { }

    // Parameterless constructor required by EF Core
    protected Customer() { }

    public static Customer CreateFromRegistration(
        Guid userId,
        string firstName,
        string lastName,
        string email)
    {
        Guard.AgainstEmptyGuid(userId, nameof(userId));
        Guard.AgainstNullOrEmpty(firstName, nameof(firstName));
        Guard.AgainstNullOrEmpty(lastName, nameof(lastName));
        Guard.AgainstNullOrEmpty(email, nameof(email));

  
        return new Customer(Guid.NewGuid())
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            KYCStatus = KYCStatus.NotStarted,
            RiskTier = RiskTier.NotAssigned,
            IsActive = true
        };
    }

    public string FullName => $"{FirstName} {LastName}";

    public bool CanOpenAccount =>
        KYCStatus == KYCStatus.Verified && IsActive;

    /// <summary>
    /// Customer completes their profile after initial registration.
    /// </summary>
    public void CompleteProfile(
        string phoneNumber,
        string nationality,
        string address,
        DateTime dateOfBirth)
    {
        PhoneNumber = Guard.AgainstNullOrEmpty(phoneNumber, nameof(phoneNumber));
        Nationality = Guard.AgainstNullOrEmpty(nationality, nameof(nationality));
        Address = Guard.AgainstNullOrEmpty(address, nameof(address));
        DateOfBirth = dateOfBirth;
    }

    /// <summary>
    /// Called when KYCApprovedEvent is consumed from AgenticWorkflow.
    /// </summary>
    public void ApproveKYC(string riskTier)
    {
        KYCStatus = KYCStatus.Verified;
        RiskTier = Enum.Parse<RiskTier>(riskTier, ignoreCase: true);
        KYCVerifiedAt = DateTime.UtcNow;
        KYCExpiresAt = DateTime.UtcNow.AddYears(2);
        KYCRejectionReason = null;
    }

    /// <summary>
    /// Called when KYCRejectedEvent is consumed from AgenticWorkflow.
    /// </summary>
    public void RejectKYC(string reason)
    {
        KYCStatus = KYCStatus.Rejected;
        KYCRejectionReason = Guard.AgainstNullOrEmpty(reason, nameof(reason));
    }

    public void Suspend() => IsActive = false;
    public void Reactivate() => IsActive = true;
}