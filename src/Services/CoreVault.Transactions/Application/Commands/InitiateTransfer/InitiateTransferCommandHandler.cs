using System.Text.Json;
using CoreVault.Contracts.Events.Transactions;
using CoreVault.SharedKernel.Primitives;
using CoreVault.Transactions.Domain.Entities;
using CoreVault.Transactions.Domain.Enums;
using CoreVault.Transactions.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Transactions.Application.Commands.InitiateTransfer;

public sealed class InitiateTransferCommandHandler: IRequestHandler<InitiateTransferCommand, Result<TransactionResponse>>
{
    private readonly TransactionsDbContext _dbContext;

    public InitiateTransferCommandHandler(TransactionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TransactionResponse>> Handle(InitiateTransferCommand command, CancellationToken cancellationToken)
    {
        //  Idempotency Check ───────
        // If we have already processed this exact request,
        // return the original result without creating a duplicate.
        // This handles retries and network failures gracefully.
        var existing = await _dbContext.Transactions.FirstOrDefaultAsync(
                t => t.IdempotencyKey == command.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            var existingFrom = await _dbContext.AccountSummaries.FirstOrDefaultAsync(
                    a => a.AccountId == existing.AccountId,
                    cancellationToken);

            var existingTo = existing.DestinationAccountId.HasValue ? await _dbContext.AccountSummaries.FirstOrDefaultAsync(
                        a => a.AccountId == existing.DestinationAccountId,
                        cancellationToken) : null;

            return Result.Success(new TransactionResponse(
                existing.Id,
                existing.Type.ToString(),
                existing.Status.ToString(),
                existing.Amount,
                existing.Currency,
                existing.Reference,
                existing.CreatedAt,
                existingFrom?.AccountNumber ?? string.Empty,
                existingTo?.AccountNumber));
        }

        // ── Resolve Sender Account 
        // Look up sender account from local AccountSummaries projection.
        // Validates ownership — account must belong to this customer.
        var fromAccount = await _dbContext.AccountSummaries
            .FirstOrDefaultAsync(
                a => a.AccountId == command.AccountId &&
                     a.CustomerId == command.CustomerId,
                cancellationToken);

        if (fromAccount is null)
            return Result.Failure<TransactionResponse>(
                Error.Create(
                    "Transaction.AccountNotFound",
                    "Source account not found or does not belong to you."));

        // Validate Sender Account Status 
        if (!fromAccount.CanTransact)
            return Result.Failure<TransactionResponse>(
                Error.Create(
                    "Transaction.AccountNotActive",
                    $"Source account is {fromAccount.Status} and cannot transact."));

        // Validate Daily Limit 
        if (command.Amount > fromAccount.DailyTransactionLimit)
            return Result.Failure<TransactionResponse>(
                Error.Create(
                    "Transaction.ExceedsDailyLimit",
                    $"Amount exceeds daily transaction limit of {fromAccount.DailyTransactionLimit} {fromAccount.Currency}."));

        //Resolve Destination Account 
        var toAccount = await _dbContext.AccountSummaries.FirstOrDefaultAsync(
                a => a.AccountId == command.DestinationAccountId,
                cancellationToken);

        if (toAccount is null)
            return Result.Failure<TransactionResponse>(
                Error.Create(
                    "Transaction.DestinationNotFound",
                    "Destination account not found."));

        //   Prevent Self-Transfer
        if (command.AccountId == command.DestinationAccountId)
            return Result.Failure<TransactionResponse>(
                Error.Create("Transaction.SelfTransfer","Cannot transfer to the same account."));

        // ─ Create Transaction 
        var transaction = Transaction.Create(
            command.AccountId,
            command.CustomerId,
            TransactionType.Transfer,
            command.Amount,
            command.IdempotencyKey,
            command.Reference,
            command.DestinationAccountId,
            command.DeviceFingerprint,
            command.IpAddress,
            command.Location);

        // ──  Create Outbox Message ────────────────────────
        // Saved in the SAME database transaction as the
        // Transaction record — Outbox Pattern guarantee.
        var outboxMessage = OutboxMessage.Create(
            nameof(TransactionInitiatedEvent),
            JsonSerializer.Serialize(new TransactionInitiatedEvent
            {
                TransactionId = transaction.Id,
                AccountId = transaction.AccountId,
                CustomerId = transaction.CustomerId,
                TransactionType = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                DestinationAccountId = transaction.DestinationAccountId,
                DeviceFingerprint = transaction.DeviceFingerprint,
                IpAddress = transaction.IpAddress,
                Location = transaction.Location,
                InitiatedAt = transaction.CreatedAt,
                CorrelationId = Guid.NewGuid()
            }));

        // ── Atomic Save ───────────────────────────────────
        // Transaction record + OutboxMessage saved together.
        // If this fails → both roll back → no orphaned records.
        // If this succeeds → background job will publish the event.
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // ── Return 202 Accepted ─────────────────────────
        // We return immediately — do not wait for AI scoring.
        // The transaction is Pending — money has NOT moved yet.
        // Client polls GET /transactions/{id} for status updates.
        return Result.Success(new TransactionResponse(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Status.ToString(),
            transaction.Amount,
            transaction.Currency,
            transaction.Reference,
            transaction.CreatedAt,
            fromAccount.AccountNumber,
            toAccount.AccountNumber));
    }
}