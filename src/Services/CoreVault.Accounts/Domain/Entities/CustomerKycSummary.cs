using CoreVault.SharedKernel.Entities;
using CoreVault.SharedKernel.Guards;

namespace CoreVault.Accounts.Domain.Entities;

/// <summary>
/// Local projection of KYC approval status.
/// Built from KYCApprovedEvent consumed from RabbitMQ.
///
/// This is Event-Carried State Transfer in action:
/// Accounts service never calls Customer service to check KYC.
/// It maintains its own local record of who is verified.
/// This record is created ONLY when KYCApprovedEvent is received.
///
/// If this record exists for a CustomerId → customer is verified.
/// If it does not exist → customer is not yet verified.
/// </summary>
public sealed class CustomerKycSummary : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string RiskTier { get; private set; } = string.Empty;
    public DateTime VerifiedAt { get; private set; }

    // Required by EF Core
    protected CustomerKycSummary() { }

    private CustomerKycSummary(Guid id) : base(id) { }

    public static CustomerKycSummary Create(
        Guid customerId,
        string fullName,
        string email,
        string riskTier,
        DateTime verifiedAt)
    {
        Guard.AgainstEmptyGuid(customerId, nameof(customerId));
        Guard.AgainstNullOrEmpty(fullName, nameof(fullName));
        Guard.AgainstNullOrEmpty(email, nameof(email));
        Guard.AgainstNullOrEmpty(riskTier, nameof(riskTier));

        return new CustomerKycSummary(Guid.NewGuid())
        {
            CustomerId = customerId,
            FullName = fullName,
            Email = email,
            RiskTier = riskTier,
            VerifiedAt = verifiedAt
        };
    }
}