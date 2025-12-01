using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        MasterDbContext masterContext,
        ILogger<DiagnosticsController> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    /// <summary>
    /// Verifica si un ApiKey es válido y retorna información del tenant
    /// Solo para desarrollo/diagnóstico - ELIMINAR EN PRODUCCIÓN
    /// </summary>
    [HttpGet("verify-apikey")]
    public async Task<IActionResult> VerifyApiKey([FromQuery] string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return BadRequest(new { message = "ApiKey is required" });
        }

        var tenant = await _masterContext.Tenants
            .Where(t => t.ApiKey == apiKey)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ApiKey,
                t.IsActive,
                t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (tenant == null)
        {
            return NotFound(new
            {
                message = "ApiKey not found",
                apiKey = apiKey.Substring(0, Math.Min(8, apiKey.Length)) + "..."
            });
        }

        return Ok(new
        {
            message = tenant.IsActive ? "ApiKey is valid" : "ApiKey exists but tenant is not active",
            tenant
        });
    }

    /// <summary>
    /// Lista todos los tenants activos con sus ApiKeys
    /// Solo para desarrollo/diagnóstico - ELIMINAR EN PRODUCCIÓN
    /// </summary>
    [HttpGet("list-tenants")]
    public async Task<IActionResult> ListTenants()
    {
        var tenants = await _masterContext.Tenants
            .Where(t => t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ApiKey,
                t.CreatedAt
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            count = tenants.Count,
            tenants
        });
    }
}
