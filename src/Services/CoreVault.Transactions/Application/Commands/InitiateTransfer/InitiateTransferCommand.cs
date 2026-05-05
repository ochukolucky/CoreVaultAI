using CoreVault.SharedKernel.Primitives;
using MediatR;

namespace CoreVault.Transactions.Application.Commands.InitiateTransfer;

public sealed record InitiateTransferCommand(
    Guid AccountId,
    Guid CustomerId,
    Guid DestinationAccountId,
    decimal Amount,
    string Reference,
    string IdempotencyKey,
    string? DeviceFingerprint,
    string? IpAddress,
    string? Location
) : IRequest<Result<TransactionResponse>>;

public sealed record TransactionResponse(
    Guid TransactionId,
    string TransactionType,
    string Status,
    decimal Amount,
    string Currency,
    string Reference,
    DateTime CreatedAt,
    string FromAccountNumber,
    string? ToAccountNumber
);