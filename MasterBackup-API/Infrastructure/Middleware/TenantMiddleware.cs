using System.Collections.Concurrent;
using System.Security.Claims;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MasterBackup_API.Infrastructure.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private static readonly ConcurrentDictionary<Guid, bool> _migrationsApplied = new();

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

            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var endpoint = $"{context.Request.Path}{context.Request.QueryString}";
            var method = context.Request.Method;

            if (tenant != null)
            {
                tenantId = tenant.Id;
                connectionString = tenant.ConnectionString;
                _logger.LogInformation("Tenant resolved from API Key: {TenantId}", tenantId);

                // Registrar log exitoso solo si NO es un heartbeat
                if (!endpoint.Contains("/heartbeat", StringComparison.OrdinalIgnoreCase))
                {
                    await LogApiKeyUsageAsync(masterDbContext, tenant.Id, remoteIp, true, null, endpoint, method);
                }
            }
            else
            {
                _logger.LogWarning("Invalid API Key provided: {ApiKey}", apiKey.Substring(0, Math.Min(8, apiKey.Length)));

                // Registrar log fallido (sin tenantId válido, usar Guid.Empty)
                await LogApiKeyUsageAsync(masterDbContext, Guid.Empty, remoteIp, false, "Invalid API Key", endpoint, method);
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
            
            // Apply pending migrations automatically
            await ApplyTenantMigrationsAsync(tenantContext, context.RequestServices);
        }

        await _next(context);

        // Clear tenant context after request
        tenantContext.Clear();
    }

    private async Task ApplyTenantMigrationsAsync(ITenantContext tenantContext, IServiceProvider serviceProvider)
    {
        // Skip if no tenant ID or migrations already applied for this tenant in this session
        if (!tenantContext.TenantId.HasValue || _migrationsApplied.ContainsKey(tenantContext.TenantId.Value))
        {
            return;
        }

        try
        {
            // Ensure connection string has SearchPath set for PostgreSQL
            var connectionString = EnsureSearchPath(tenantContext.ConnectionString);
            
            // Create TenantDbContext manually with the current tenant context
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            
            using var tenantDbContext = new TenantDbContext(optionsBuilder.Options, tenantContext);
            
            // Check if there are pending migrations
            var pendingMigrations = await tenantDbContext.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                _logger.LogWarning("Applying {Count} pending migration(s) to tenant {TenantId}: {Migrations}", 
                    pendingMigrations.Count(), 
                    tenantContext.TenantId,
                    string.Join(", ", pendingMigrations));
                
                await tenantDbContext.Database.MigrateAsync();
                
                _logger.LogInformation("Successfully applied {Count} migration(s) to tenant {TenantId}", 
                    pendingMigrations.Count(), 
                    tenantContext.TenantId);
            }
            else
            {
                _logger.LogDebug("No pending migrations for tenant {TenantId}", tenantContext.TenantId);
            }
            
            // Mark as applied (cache for this session)
            _migrationsApplied.TryAdd(tenantContext.TenantId.Value, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations to tenant {TenantId}: {Message}", 
                tenantContext.TenantId, 
                ex.Message);
            // Don't throw - let the request continue even if migrations fail
            // But don't cache the result so we can retry on next request
        }
    }

    /// <summary>
    /// Clear the migrations cache for a specific tenant or all tenants.
    /// Useful when deploying new migrations or for testing.
    /// </summary>
    public static void ClearMigrationsCache(Guid? tenantId = null)
    {
        if (tenantId.HasValue)
        {
            _migrationsApplied.TryRemove(tenantId.Value, out _);
        }
        else
        {
            _migrationsApplied.Clear();
        }
    }

    /// <summary>
    /// Registrar uso de API key en la tabla de logs
    /// </summary>
    private async Task LogApiKeyUsageAsync(
        MasterDbContext masterDbContext,
        Guid tenantId,
        string remoteIp,
        bool success,
        string? errorMessage,
        string? endpoint,
        string? method)
    {
        try
        {
            var log = new Domain.Entities.ApiKeyLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RemoteIp = remoteIp,
                Success = success,
                ErrorMessage = errorMessage,
                Endpoint = endpoint,
                Method = method,
                CreatedAt = DateTime.UtcNow
            };

            masterDbContext.ApiKeyLogs.Add(log);
            await masterDbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar log de API key para tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Ensure the connection string has SearchPath=public set for PostgreSQL.
    /// This prevents "no schema has been selected to create in" errors during migrations.
    /// </summary>
    private static string EnsureSearchPath(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        
        // Only set SearchPath if not already set
        if (string.IsNullOrEmpty(builder.SearchPath))
        {
            builder.SearchPath = "public";
        }
        
        return builder.ConnectionString;
    }
}
