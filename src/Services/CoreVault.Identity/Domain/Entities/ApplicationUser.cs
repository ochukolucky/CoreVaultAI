using CoreVault.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace CoreVault.Identity.Domain.Entities;

/// <summary>
/// The core identity entity. Extends IdentityUser (which gives us
/// email, password hash, lockout, MFA built-in) and adds
/// our own banking-specific fields on top.
///
/// Why extend IdentityUser instead of building from scratch?
/// ASP.NET Core Identity handles password hashing, lockout policies,
/// email confirmation, and token generation — all security-critical
/// features that should never be hand-rolled in a financial system.
/// We extend it with our domain fields rather than reinventing it.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsMFAEnabled { get; private set; }
    public string? MFASecretKey { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    // Required by EF Core
    private ApplicationUser() { }

    public static ApplicationUser Create(
        string firstName,
        string lastName,
        string email,
        UserRole role = UserRole.Customer)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false,
            IsMFAEnabled = false
        };
    }

    public string FullName => $"{FirstName} {LastName}";

    public void RecordLogin() => LastLoginAt = DateTime.UtcNow;

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public bool IsRefreshTokenValid(string token) =>
        RefreshToken == token &&
        RefreshTokenExpiry > DateTime.UtcNow;

    public void EnableMFA(string secretKey)
    {
        IsMFAEnabled = true;
        MFASecretKey = secretKey;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}