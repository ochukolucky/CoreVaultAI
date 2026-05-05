using CoreVault.Accounts.Domain.Enums;

namespace CoreVault.Accounts.Application.DTOs
{

    /// <summary>
    /// Request body for opening an account.
    /// CustomerId is NOT here — it comes from the JWT token.
    /// </summary>
    public sealed record OpenAccountRequest(
        AccountType AccountType,
        decimal InitialDeposit
    );
}
