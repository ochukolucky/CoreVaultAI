using CoreVault.Identity.Application.DTOs;
using CoreVault.Identity.Domain.Entities;
using CoreVault.Identity.Infrastructure.Services;
using CoreVault.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CoreVault.Identity.Application.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        // 1 — Find user
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user is null)
            return Result.Failure<AuthTokenDto>(
                Error.Auth.InvalidCredentials);

        // 2 — Check account is active
        if (!user.IsActive)
            return Result.Failure<AuthTokenDto>(
                Error.Auth.AccountLocked);

        // 3 — Check lockout (Identity handles failed attempt tracking)
        if (await _userManager.IsLockedOutAsync(user))
            return Result.Failure<AuthTokenDto>(
                Error.Auth.AccountLocked);

        // 4 — Verify password
        var passwordValid = await _userManager
            .CheckPasswordAsync(user, command.Password);

        if (!passwordValid)
        {
            // Record failed attempt — Identity auto-locks after threshold
            await _userManager.AccessFailedAsync(user);
            return Result.Failure<AuthTokenDto>(
                Error.Auth.InvalidCredentials);
        }

        // 5 — Reset failed attempts on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // 6 — Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        user.RecordLogin();
        await _userManager.UpdateAsync(user);

        return Result.Success(new AuthTokenDto(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            user.FullName,
            user.Role.ToString()));
    }
}