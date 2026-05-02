using CoreVault.Identity.Application.Commands.Login;
using CoreVault.Identity.Application.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreVault.Identity.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) =>
       _sender = sender;

    /// <summary>Register a new customer account</summary>

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { result.Error.Code, result.Error.Message });
    }

    /// <summary>Login and receive JWT tokens</summary>

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.Error.Code.Contains("Locked")
                ? Forbid()
                : Unauthorized(new
                {
                    result.Error.Code,
                    result.Error.Message
                });

        return Ok(result.Value);
    }

    /// <summary> Health check — verify token is valid</summary>

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {

        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var email = User.FindFirst("email")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        var role = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var firstName = User.FindFirst("firstName")?.Value;
        var lastName = User.FindFirst("lastName")?.Value;

        return Ok(new { userId, email, role, firstName, lastName });
    }

}