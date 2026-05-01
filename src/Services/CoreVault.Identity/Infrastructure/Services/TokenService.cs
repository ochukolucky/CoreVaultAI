using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoreVault.Identity.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CoreVault.Identity.Infrastructure.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
}

/// <summary>
/// Generates JWT access tokens and refresh tokens.
///
/// Access token  → short-lived (15 mins), sent with every API request
/// Refresh token → long-lived (7 days), used ONLY to get a new access token
///
/// Why short access tokens?
/// If an access token is stolen, the attacker has 15 minutes maximum.
/// After that, they need the refresh token to continue — which is
/// stored securely and can be revoked server-side.
/// This is the industry standard for financial applications.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(ApplicationUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]!);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims are key-value pairs embedded in the token.
        // Every service that validates this token can read these
        // without calling the Identity service again.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("role", user.Role.ToString()),
            new("isActive", user.IsActive.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Cryptographically secure random bytes — not predictable
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}