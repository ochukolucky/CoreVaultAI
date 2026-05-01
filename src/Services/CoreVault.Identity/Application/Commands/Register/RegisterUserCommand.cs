using CoreVault.Identity.Application.DTOs;
using CoreVault.SharedKernel.Primitives;
using MediatR;

namespace CoreVault.Identity.Application.Commands.Register;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword
) : IRequest<Result<AuthTokenDto>>;