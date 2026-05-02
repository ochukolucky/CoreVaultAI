using CoreVault.Accounts.Domain.Enums;
using CoreVault.SharedKernel.Primitives;
using MediatR;

namespace CoreVault.Accounts.Application.Commands.OpenAccount;

/// <summary>
/// Command to open a new bank account.
/// CustomerId is taken from the JWT token claim —
/// not from the request body. This prevents a customer
/// from opening an account on behalf of another customer.
/// </summary>
public sealed record OpenAccountCommand(
    Guid CustomerId,
    AccountType AccountType,
    decimal InitialDeposit
) : IRequest<Result<OpenAccountResponse>>;

public sealed record OpenAccountResponse(
    Guid AccountId,
    string AccountNumber,
    string AccountType,
    string Status,
    decimal Balance,
    string Currency,
    DateTime CreatedAt
);