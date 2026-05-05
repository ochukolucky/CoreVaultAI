using CoreVault.Accounts.Domain.Entities;
using CoreVault.Accounts.Domain.Enums;
using CoreVault.Accounts.Infrastructure.Persistence;
using CoreVault.Contracts.Events.Accounts;
using CoreVault.Accounts.Infrastructure.Messaging;
using CoreVault.SharedKernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Accounts.Application.Commands.OpenAccount;

public sealed class OpenAccountCommandHandler
    : IRequestHandler<OpenAccountCommand, Result<OpenAccountResponse>>
{
    private readonly AccountsDbContext _dbContext; 
    private readonly IEventPublisher _eventPublisher; 

    public OpenAccountCommandHandler(
        AccountsDbContext dbContext,
        IEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<OpenAccountResponse>> Handle(OpenAccountCommand command,CancellationToken cancellationToken)
    {
        // ── Step 1: Check KYC verification ───────────────────────
        // We check our LOCAL CustomerKycSummaries table —
        // not the Customer service. This is Event-Carried State Transfer.
        // If the record exists, this customer passed KYC verification.
        var kycSummary = await _dbContext.CustomerKycSummaries
            .FirstOrDefaultAsync(k => k.CustomerId == command.CustomerId, cancellationToken);

        if (kycSummary is null)
            return Result.Failure<OpenAccountResponse>(
                Error.Create("Account.KYCNotVerified", "You must complete KYC verification before opening an account."));

        // ── Step 2: Apply product rules based on account type ────
        var (interestRate, dailyLimit) = GetProductConfig(command.AccountType);

        // ── Step 3: Open the account ─────────────────────────────
        var account = Account.Open(
            command.CustomerId,
            command.AccountType,
            command.InitialDeposit,
            interestRate,
            dailyLimit);

        // ── Step 4: Persist ──────────────────────────────────────
        await _dbContext.Accounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── Step 5: Publish event ────────────────────────────────
        await _eventPublisher.PublishAsync(new AccountOpenedEvent
        {
            AccountId = account.Id,
            CustomerId = account.CustomerId,
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType.ToString(),
            InitialBalance = account.Balance,
            Currency = account.Currency,
            DailyTransactionLimit = account.DailyTransactionLimit,
            OpenedAt = account.CreatedAt,
            CorrelationId = Guid.NewGuid()
        });

        return Result.Success(new OpenAccountResponse(
            account.Id,
            account.AccountNumber,
            account.AccountType.ToString(),
            account.Status.ToString(),
            account.Balance,
            account.Currency,
            account.CreatedAt));
    }

    // ── Product Configuration ────────────────────────────────────
    // In a real bank this would come from a ProductConfig table.
    // For CoreVault AI we keep it simple and hardcoded per type.
    private static (decimal interestRate, decimal dailyLimit)
        GetProductConfig(AccountType accountType) =>
        accountType switch
        {
            AccountType.Savings =>
                (interestRate: 2.5m, dailyLimit: 10_000m),
            AccountType.Current =>
                (interestRate: 0.0m, dailyLimit: 50_000m),
            AccountType.FixedDeposit =>
                (interestRate: 4.5m, dailyLimit: 0m),
            _ => throw new ArgumentOutOfRangeException(
                nameof(accountType),
                "Unknown account type.")
        };
}