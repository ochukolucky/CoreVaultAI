using CoreVault.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Customer.API.Controllers;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _dbContext;

    public CustomersController(CustomerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Get customer profile by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
            return NotFound(new
            {
                Code = "Customer.NotFound",
                Message = $"Customer {id} not found."
            });

        return Ok(new
        {
            customer.Id,
            customer.UserId,
            customer.FullName,
            customer.Email,
            customer.PhoneNumber,
            KYCStatus = customer.KYCStatus.ToString(),
            RiskTier = customer.RiskTier.ToString(),
            customer.CanOpenAccount,
            customer.CreatedAt
        });
    }

    /// <summary>Get customer by UserId — used internally</summary>
    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetByUserId(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (customer is null)
            return NotFound(new
            {
                Code = "Customer.NotFound",
                Message = $"No customer profile found for user {userId}."
            });

        return Ok(new
        {
            customer.Id,
            customer.UserId,
            customer.FullName,
            customer.Email,
            KYCStatus = customer.KYCStatus.ToString(),
            RiskTier = customer.RiskTier.ToString(),
            customer.CanOpenAccount,
            customer.CreatedAt
        });
    }
}