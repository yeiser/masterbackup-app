using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
using MasterBackup_API.Application.Features.Users.Commands;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;
using System.Security.Claims;
using FluentValidation;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;
    private readonly IValidator<InviteUserDto> _inviteUserValidator;

    public UsersController(IMediator mediator, ILogger<UsersController> logger, IValidator<InviteUserDto> inviteUserValidator)
    {
        _mediator = mediator;
        _logger = logger;
        _inviteUserValidator = inviteUserValidator;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new GetUserProfileCommand(userId);
        var result = await _mediator.Send(command);

        if (result == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new UpdateUserProfileCommand(userId, updateProfileDto);
        var result = await _mediator.Send(command);

        if (result == null)
        {
            return BadRequest(new { message = "No se pudo actualizar el perfil" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify current password
    /// </summary>
    [HttpPost("verify-password")]
    public async Task<IActionResult> VerifyPassword([FromBody] VerifyPasswordDto verifyPasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new VerifyPasswordCommand(userId, verifyPasswordDto.Password);
        var result = await _mediator.Send(command);

        return Ok(new { valid = result });
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new ChangePasswordCommand(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "No se pudo cambiar la contraseña. Verifica que la contraseña actual sea correcta." });
        }

        return Ok(new { message = "Contraseña actualizada exitosamente" });
    }

    /// <summary>
    /// Toggle two-factor authentication
    /// </summary>
    [HttpPost("toggle-2fa")]
    public async Task<IActionResult> Toggle2FA([FromBody] Toggle2FADto toggle2FADto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var command = new Toggle2FACommand(userId, toggle2FADto.Enable);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "No se pudo actualizar la configuración de 2FA" });
        }

        return Ok(new { enabled = toggle2FADto.Enable, message = $"Autenticación de dos factores {(toggle2FADto.Enable ? "activada" : "desactivada")} exitosamente" });
    }

    /// <summary>
    /// Invite a new user to the tenant (Admin only)
    /// </summary>
    [HttpPost("invite")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserDto inviteUserDto)
    {
        var validationResult = await _inviteUserValidator.ValidateAsync(inviteUserDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdStr = User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized();
        }

        var command = new InviteUserCommand(inviteUserDto.Email, inviteUserDto.Role, userId, tenantId);

        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Error al enviar la invitación. El usuario ya existe o tiene una invitación pendiente." });
        }

        return Ok(new { message = "Invitación enviada exitosamente" });
    }
}
