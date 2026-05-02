using CoreVault.Contracts.Events.Identity;
using CoreVault.Identity.Application.DTOs;
using CoreVault.Identity.Domain.Entities;
using CoreVault.Identity.Domain.Enums;
using CoreVault.Identity.Infrastructure.Messaging;
using CoreVault.Identity.Infrastructure.Services;
using CoreVault.SharedKernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CoreVault.Identity.Application.Commands.Register;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthTokenDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;

    public RegisterUserCommandHandler
    (
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IEventPublisher eventPublisher
    )
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<AuthTokenDto>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // 1 — Check passwords match
        if (command.Password != command.ConfirmPassword)
            return Result.Failure<AuthTokenDto>(
                Error.Create("Register.PasswordMismatch",
                    "Passwords do not match."));

        // 2 — Check email is not already registered
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            return Result.Failure<AuthTokenDto>(
                Error.Create("Register.EmailTaken",
                    "An account with this email already exists."));

        // 3 — Create user entity via factory (never new directly)
        var user = ApplicationUser.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            UserRole.Customer);

        // 4 — Persist via Identity (handles password hashing)
        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ",
                result.Errors.Select(e => e.Description));
            return Result.Failure<AuthTokenDto>(
                Error.Create("Register.Failed", errors));
        }

        // 5 — Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiry = DateTime.UtcNow.AddMinutes(15);

        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _userManager.UpdateAsync(user);

        // 6 — Publish domain event so Customer service
        //     creates the customer profile automatically
        await _eventPublisher.PublishAsync(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            RegisteredAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid()
        });

        return Result.Success(new AuthTokenDto(
            accessToken,
            refreshToken,
            expiry,
            user.FullName,
            user.Role.ToString()));
    }
}