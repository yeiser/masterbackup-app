using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new tenant and admin user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new RegisterCommand(
            registerDto.Email,
            registerDto.Password,
            registerDto.FirstName,
            registerDto.LastName,
            registerDto.TenantName,
            registerDto.Subdomain,
            registerDto.EnableTwoFactor
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new LoginCommand(loginDto.Email, loginDto.Password, loginDto.TwoFactorCode);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.RequiresTwoFactor)
            {
                return Ok(new { requiresTwoFactor = true, message = result.Message });
            }
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new ForgotPasswordCommand(forgotPasswordDto.Email);

        await _mediator.Send(command);

        return Ok(new { message = "If your email exists, you will receive a password reset link" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new ResetPasswordCommand(resetPasswordDto.Email, resetPasswordDto.Token, resetPasswordDto.NewPassword);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Invalid or expired reset token" });
        }

        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Accept invitation and create account
    /// </summary>
    [HttpPost("accept-invitation")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto acceptInvitationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new AcceptInvitationCommand(
            acceptInvitationDto.Token,
            acceptInvitationDto.Password,
            acceptInvitationDto.FirstName,
            acceptInvitationDto.LastName,
            acceptInvitationDto.EnableTwoFactor
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    [Authorize]
    [HttpPost("enable-2fa")]
    public async Task<IActionResult> Enable2FA()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new Enable2FACommand(userId);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Failed to enable 2FA" });
        }

        return Ok(new { message = "2FA enabled successfully" });
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [Authorize]
    [HttpPost("disable-2fa")]
    public async Task<IActionResult> Disable2FA()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new Disable2FACommand(userId);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Failed to disable 2FA" });
        }

        return Ok(new { message = "2FA disabled successfully" });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var tenantId = User.FindFirst("TenantId")?.Value;

        return Ok(new
        {
            userId,
            email,
            role,
            tenantId
        });
    }
}
