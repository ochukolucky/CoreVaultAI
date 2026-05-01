namespace CoreVault.Identity.Application.DTOs;

public sealed record RegisterUserDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword
);