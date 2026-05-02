using CoreVault.Customer.Application.DTOs;
using CoreVault.SharedKernel.Primitives;
using MediatR;

namespace CoreVault.Customer.Application.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email
) : IRequest<Result<CustomerDto>>;