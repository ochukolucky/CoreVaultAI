using CoreVault.Accounts.Application.Commands.OpenAccount;
using CoreVault.Accounts.Domain.Enums;
using CoreVault.Accounts.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Accounts.API.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly AccountsDbContext _dbContext;

    public AccountsController(ISender sender, AccountsDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
    }

    /// <summary>Open a new bank account</summary>
    [HttpPost]
    public async Task<IActionResult> OpenAccount([FromBody] OpenAccountRequest request,CancellationToken cancellationToken)
    {
        // Extract CustomerId from JWT token claims
        // Never trust CustomerId from request body
        var customerIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) ||
            !Guid.TryParse(customerIdClaim, out var userId))
            return Unauthorized(new
            {
                Code = "Auth.InvalidToken",
                Message = "Unable to resolve user identity from token."
            });

        // Note: userId from token is the Identity UserId
        // We need to resolve it to CustomerId via our KYC summaries
        // For now we treat userId as customerId — in a full
        // implementation the KYC summary would store both
        var command = new OpenAccountCommand(
            userId,
            request.AccountType,
            request.InitialDeposit);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value)
            : BadRequest(new
            {
                result.Error.Code,
                result.Error.Message
            });
    }

    /// <summary>Get all accounts for the authenticated customer</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAccounts(CancellationToken cancellationToken)
    {
        var customerIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) ||
            !Guid.TryParse(customerIdClaim, out var userId))
            return Unauthorized();

        var accounts = await _dbContext.Accounts.Where(a => a.CustomerId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.AccountNumber,
                AccountType = a.AccountType.ToString(),
                Status = a.Status.ToString(),
                a.Balance,
                a.Currency,
                a.InterestRate,
                a.DailyTransactionLimit,
                a.CreatedAt,
                a.LastTransactionAt
            })
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    /// <summary>Get a specific account by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customerIdClaim = User.FindFirst("sub")?.Value?? User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) ||
            !Guid.TryParse(customerIdClaim, out var userId))
            return Unauthorized();

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == userId, cancellationToken);

        if (account is null)
            return NotFound(new
            {
                Code = "Account.NotFound",
                Message = $"Account {id} not found."
            });

        return Ok(new
        {
            account.Id,
            account.AccountNumber,
            AccountType = account.AccountType.ToString(),
            Status = account.Status.ToString(),
            account.Balance,
            account.Currency,
            account.InterestRate,
            account.DailyTransactionLimit,
            account.CreatedAt,
            account.LastTransactionAt
        });
    }

    /// <summary>Get account balance</summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken cancellationToken)
    {
        var customerIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(
               System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var userId))
            return Unauthorized();

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == userId, cancellationToken);
        if (account is null)
            return NotFound(new
            {
                Code = "Account.NotFound",
                Message = $"Account {id} not found."
            });

        return Ok(new
        {
            account.AccountNumber,
            account.Balance,
            account.Currency,
            AsOf = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Request body for opening an account.
/// CustomerId is NOT here — it comes from the JWT token.
/// </summary>
public sealed record OpenAccountRequest(
    AccountType AccountType,
    decimal InitialDeposit
);