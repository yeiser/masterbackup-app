using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Infrastructure.Middleware;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller para operaciones administrativas del sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;

    public SystemController(ILogger<SystemController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Limpia el caché de migraciones para forzar su reaplicación
    /// </summary>
    /// <param name="tenantId">ID del tenant (opcional, si no se proporciona limpia todo el caché)</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPost("clear-migrations-cache")]
    [RoleAuthorization(UserRole.Admin)]
    public IActionResult ClearMigrationsCache([FromQuery] Guid? tenantId = null)
    {
        try
        {
            TenantMiddleware.ClearMigrationsCache(tenantId);
            
            var message = tenantId.HasValue 
                ? $"Caché de migraciones limpiado para tenant {tenantId}" 
                : "Caché de migraciones limpiado para todos los tenants";
            
            _logger.LogInformation(message);
            
            return Ok(new { message, tenantId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error limpiando caché de migraciones");
            return StatusCode(500, new { message = "Error al limpiar el caché de migraciones" });
        }
    }

    /// <summary>
    /// Obtiene información del sistema
    /// </summary>
    [HttpGet("info")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public IActionResult GetSystemInfo()
    {
        return Ok(new
        {
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            serverTime = DateTime.UtcNow,
            serverTimeZone = TimeZoneInfo.Local.DisplayName
        });
    }
}
