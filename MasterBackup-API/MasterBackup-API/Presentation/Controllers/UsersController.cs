using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Auth.Commands;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Invite a new user to the tenant (Admin only)
    /// </summary>
    [HttpPost("invite")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserDto inviteUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
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
            return BadRequest(new { message = "Failed to send invitation. User may already exist or have a pending invitation." });
        }

        return Ok(new { message = "Invitation sent successfully" });
    }
}
