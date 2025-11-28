using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
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
