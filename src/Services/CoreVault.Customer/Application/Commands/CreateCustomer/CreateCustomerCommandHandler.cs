using CoreVault.Customer.Application.DTOs;
using CoreVault.Customer.Infrastructure.Persistence;
using CoreVault.SharedKernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = CoreVault.Customer.Domain.Entities.Customer;

namespace CoreVault.Customer.Application.Commands.CreateCustomer;

public sealed class CreateCustomerCommandHandler
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly CustomerDbContext _dbContext;

    public CreateCustomerCommandHandler(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Idempotency check — if customer already exists for this
        // UserId, return success without creating a duplicate.
        // This handles the case where the event is delivered twice
        // (at-least-once delivery from RabbitMQ).
        var existing = await _dbContext.Customers
            .FirstOrDefaultAsync(
                c => c.UserId == command.UserId,
                cancellationToken);

        if (existing is not null)
            return Result.Success(MapToDto(existing));

        var customer = CustomerEntity.CreateFromRegistration(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.Email);

        await _dbContext.Customers.AddAsync(customer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(customer));
    }

    private static CustomerDto MapToDto(CustomerEntity c) =>
        new(
            c.Id,
            c.UserId,
            c.FullName,
            c.Email,
            c.PhoneNumber,
            c.KYCStatus.ToString(),
            c.RiskTier.ToString(),
            c.CanOpenAccount,
            c.CreatedAt
        );
}