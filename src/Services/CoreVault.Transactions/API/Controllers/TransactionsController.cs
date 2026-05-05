using CoreVault.Transactions.Application.Commands.InitiateTransfer;
using CoreVault.Transactions.Application.DTOs;
using CoreVault.Transactions.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Transactions.API.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly TransactionsDbContext _dbContext;

    public TransactionsController(ISender sender,TransactionsDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
    }

    /// <summary>Initiate a transfer between two accounts</summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromBody] TransferRequest request,CancellationToken cancellationToken
    )
    {
        // Validate idempotency key is present
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new
            {
                Code = "Transaction.MissingIdempotencyKey",
                Message = "Idempotency-Key header is required."
            });

        // Extract CustomerId from JWT token
        var customerIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) ||
            !Guid.TryParse(customerIdClaim, out var customerId))
            return Unauthorized();

        // Extract device context from request headers
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var command = new InitiateTransferCommand(
            request.AccountId,
            customerId,
            request.DestinationAccountId,
            request.Amount,
            request.Reference,
            idempotencyKey,
            userAgent,
            ipAddress,
            request.Location);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Accepted(result.Value)  // 202 — pending AI scoring
            : BadRequest(new
            {
                result.Error.Code,
                result.Error.Message
            });
    }

    /// <summary>Get transaction by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customerIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            return Unauthorized();

        var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(
                t => t.Id == id && t.CustomerId == customerId,
                cancellationToken);

        if (transaction is null)
            return NotFound(new
            {
                Code = "Transaction.NotFound",
                Message = $"Transaction {id} not found."
            });

        // Resolve account numbers from local projection
        var fromAccount = await _dbContext.AccountSummaries
            .FirstOrDefaultAsync(
                a => a.AccountId == transaction.AccountId,
                cancellationToken);

        var toAccount = transaction.DestinationAccountId.HasValue
            ? await _dbContext.AccountSummaries
                .FirstOrDefaultAsync(
                    a => a.AccountId == transaction.DestinationAccountId,
                    cancellationToken)
            : null;

        return Ok(new
        {
            transaction.Id,
            TransactionType = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            transaction.Amount,
            transaction.Currency,
            transaction.Reference,
            transaction.CreatedAt,
            transaction.CompletedAt,
            transaction.RiskScore,
            FromAccountNumber = fromAccount?.AccountNumber,
            ToAccountNumber = toAccount?.AccountNumber,
            transaction.FailureReason,
            transaction.FraudReason
        });
    }

    /// <summary>Get all transactions for authenticated customer</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) ||
            !Guid.TryParse(customerIdClaim, out var customerId))
            return Unauthorized();

        var totalCount = await _dbContext.Transactions
            .CountAsync(
                t => t.CustomerId == customerId,
                cancellationToken);

        var transactions = await _dbContext.Transactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                TransactionType = t.Type.ToString(),
                Status = t.Status.ToString(),
                t.Amount,
                t.Currency,
                t.Reference,
                t.CreatedAt,
                t.CompletedAt,
                t.RiskScore
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = transactions
        });
    }
}

