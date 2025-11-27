using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller refactorizado usando Clean Architecture + CQRS con MediatR
/// Este es un ejemplo de cómo deben verse los controllers con la nueva arquitectura
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class AuthControllerRefactored : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthControllerRefactored> _logger;

    public AuthControllerRefactored(IMediator mediator, ILogger<AuthControllerRefactored> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new tenant and admin user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new RegisterCommand(
            dto.Email,
            dto.Password,
            dto.FirstName,
            dto.LastName,
            dto.TenantName,
            dto.Subdomain,
            dto.EnableTwoFactor
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
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new LoginCommand(dto.Email, dto.Password, dto.TwoFactorCode);

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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new ForgotPasswordCommand(dto.Email);

        await _mediator.Send(command);

        return Ok(new { message = "If your email exists, you will receive a password reset link" });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new ResetPasswordCommand(dto.Email, dto.Token, dto.NewPassword);

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
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new AcceptInvitationCommand(
            dto.Token,
            dto.Password,
            dto.FirstName,
            dto.LastName,
            dto.EnableTwoFactor
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
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
