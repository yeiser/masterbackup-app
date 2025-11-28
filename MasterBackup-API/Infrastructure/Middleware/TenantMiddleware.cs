using System.Security.Claims;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, MasterDbContext masterDbContext)
    {
        Guid? tenantId = null;
        string? connectionString = null;

        // 1. Try to resolve tenant from API Key header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValue))
        {
            var apiKey = apiKeyValue.ToString();
            var tenant = await masterDbContext.Tenants
                .Where(t => t.ApiKey == apiKey && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant != null)
            {
                tenantId = tenant.Id;
                connectionString = tenant.ConnectionString;
                _logger.LogInformation("Tenant resolved from API Key: {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Invalid API Key provided: {ApiKey}", apiKey.Substring(0, Math.Min(8, apiKey.Length)));
            }
        }
        // 2. Try to resolve tenant from JWT token
        else if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var parsedTenantId))
            {
                var tenant = await masterDbContext.Tenants
                    .Where(t => t.Id == parsedTenantId && t.IsActive)
                    .FirstOrDefaultAsync();

                if (tenant != null)
                {
                    tenantId = tenant.Id;
                    connectionString = tenant.ConnectionString;
                    _logger.LogInformation("Tenant resolved from JWT: {TenantId}", tenantId);
                }
            }
        }

        // Set tenant context if resolved
        if (tenantId.HasValue && !string.IsNullOrEmpty(connectionString))
        {
            tenantContext.SetTenant(tenantId.Value, connectionString);
            context.Items["TenantId"] = tenantId.Value.ToString();
        }

        await _next(context);

        // Clear tenant context after request
        tenantContext.Clear();
    }
}
