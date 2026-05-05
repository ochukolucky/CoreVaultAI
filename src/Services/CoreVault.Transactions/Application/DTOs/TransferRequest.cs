namespace CoreVault.Transactions.Application.DTOs;
public sealed record TransferRequest(
    Guid AccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Reference,
    string? Location
);