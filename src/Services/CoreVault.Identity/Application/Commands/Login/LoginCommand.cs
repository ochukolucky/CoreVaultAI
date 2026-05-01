using CoreVault.Identity.Application.DTOs;
using CoreVault.SharedKernel.Primitives;
using MediatR;

namespace CoreVault.Identity.Application.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<AuthTokenDto>>;