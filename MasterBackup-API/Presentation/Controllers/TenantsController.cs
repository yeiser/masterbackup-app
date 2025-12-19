using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Features.Tenants.Commands;
using MasterBackup_API.Application.Features.Tenants.Queries;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IMediator mediator, ILogger<TenantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTenant()
    {
        var tenantIdStr = User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized(new { message = "TenantId no encontrado en el token" });
        }

        var query = new GetTenantQuery(tenantId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "Tenant no encontrado" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Update tenant information (Admin only)
    /// </summary>
    [HttpPut]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> UpdateTenant([FromBody] UpdateTenantDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tenantIdStr = User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized(new { message = "TenantId no encontrado en el token" });
        }

        var command = new UpdateTenantCommand(tenantId, dto);
        var result = await _mediator.Send(command);

        if (result == null)
        {
            return NotFound(new { message = "Tenant no encontrado o error al actualizar" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get tenant API key
    /// </summary>
    [HttpGet("apikey")]
    public async Task<IActionResult> GetApiKey()
    {
        var tenantIdStr = User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized(new { message = "TenantId no encontrado en el token" });
        }

        var query = new GetTenantApiKeyQuery(tenantId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "API key no encontrada" });
        }

        return Ok(new { apiKey = result });
    }

    /// <summary>
    /// Get API key connection logs with pagination
    /// </summary>
    [HttpGet("apikey/logs")]
    public async Task<IActionResult> GetApiKeyLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantIdStr = User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Unauthorized(new { message = "TenantId no encontrado en el token" });
        }

        if (pageSize > 100) pageSize = 100; // Límite máximo de 100 registros por página

        var query = new GetApiKeyLogsQuery(tenantId, page, pageSize);
        var result = await _mediator.Send(query);

        return Ok(result);
    }
}
