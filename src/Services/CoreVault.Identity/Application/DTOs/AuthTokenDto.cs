namespace CoreVault.Identity.Application.DTOs;

public sealed record AuthTokenDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string FullName,
    string Role
);