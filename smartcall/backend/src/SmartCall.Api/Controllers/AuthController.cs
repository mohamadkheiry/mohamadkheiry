using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartCall.Application.Features.Auth;

namespace SmartCall.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<AuthResultDto> Register(RegisterCommand command, CancellationToken ct)
        => await mediator.Send(command, ct);

    [HttpPost("login")]
    public async Task<AuthResultDto> Login(LoginCommand command, CancellationToken ct)
        => await mediator.Send(command, ct);

    public record ForgotPasswordRequest(string Email);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        await mediator.Send(new ForgotPasswordCommand(request.Email, origin), ct);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return Ok(new { message = "Password has been reset." });
    }
}
