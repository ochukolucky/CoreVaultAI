namespace CoreVault.Customer.Application.DTOs;

public sealed record CustomerDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    string KYCStatus,
    string RiskTier,
    bool CanOpenAccount,
    DateTime CreatedAt
);