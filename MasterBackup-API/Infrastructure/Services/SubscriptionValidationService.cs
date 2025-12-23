using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// Servicio para validar límites de suscripción del tenant
/// </summary>
public class SubscriptionValidationService : ISubscriptionValidationService
{
    private readonly MasterDbContext _masterContext;
    private readonly TenantDbContext _tenantContext;
    private readonly ITenantContext _tenantContextService;
    private readonly ILogger<SubscriptionValidationService> _logger;

    public SubscriptionValidationService(
        MasterDbContext masterContext,
        TenantDbContext tenantContext,
        ITenantContext tenantContextService,
        ILogger<SubscriptionValidationService> logger)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
        _tenantContextService = tenantContextService;
        _logger = logger;
    }

    public async Task<(bool CanCreate, string? ErrorMessage)> CanCreateDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return (false, "Tenant not found");
        }

        // Obtener suscripción activa
        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return (false, "No active subscription found");
        }

        // Verificar límite de bases de datos
        var maxDatabases = subscription.Plan.MaxDatabases;
        if (maxDatabases == -1) // Ilimitado
        {
            return (true, null);
        }

        // Contar bases de datos actuales
        var currentCount = await _tenantContext.DatabaseConnections
            .Where(dc => dc.IsActive)
            .CountAsync(cancellationToken);

        if (currentCount >= maxDatabases)
        {
            return (false, $"Database limit reached. Your plan allows {maxDatabases} database(s). Upgrade to add more.");
        }

        return (true, null);
    }

    public async Task<(bool CanCreate, string? ErrorMessage)> CanCreateUserAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return (false, "Tenant not found");
        }

        // Obtener suscripción activa
        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return (false, "No active subscription found");
        }

        // Verificar límite de usuarios
        var maxUsers = subscription.Plan.MaxUsers;
        if (maxUsers == -1) // Ilimitado
        {
            return (true, null);
        }

        // Contar usuarios actuales
        var currentCount = await _masterContext.Users
            .Where(u => u.TenantId == tenantId.Value && u.IsActive)
            .CountAsync(cancellationToken);

        if (currentCount >= maxUsers)
        {
            return (false, $"User limit reached. Your plan allows {maxUsers} user(s). Upgrade to add more.");
        }

        return (true, null);
    }

    public async Task<(bool CanCreate, string? ErrorMessage)> CanCreateScheduledBackupAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return (false, "Tenant not found");
        }

        // Obtener suscripción activa
        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return (false, "No active subscription found");
        }

        // Verificar si tiene backups programados habilitados
        if (!subscription.Plan.ScheduledBackupsEnabled)
        {
            return (false, "Scheduled backups are not available in your plan. Upgrade to enable this feature.");
        }

        return (true, null);
    }

    public async Task<(bool HasSpace, string? ErrorMessage, decimal UsedGB, long MaxGB)> CheckStorageAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return (false, "Tenant not found", 0, 0);
        }

        // Obtener suscripción activa
        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return (false, "No active subscription found", 0, 0);
        }

        var maxStorageGB = subscription.Plan.MaxStorageGB;

        // Calcular almacenamiento usado
        decimal storageUsedGB = 0;
        if (!string.IsNullOrEmpty(_tenantContextService.ConnectionString))
        {
            try
            {
                using (var connection = new Npgsql.NpgsqlConnection(_tenantContextService.ConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    // Calcular almacenamiento usado (suma de tamaños de backups completados)
                    using (var cmd = new Npgsql.NpgsqlCommand(
                        @"SELECT COALESCE(SUM(""BackupSize""), 0) 
                          FROM ""BackupHistories"" 
                          WHERE ""Status"" = 3", connection)) // BackupStatus.Completed = 3
                    {
                        var result = await cmd.ExecuteScalarAsync(cancellationToken);
                        var totalBackupSize = result != null ? Convert.ToInt64(result) : 0;
                        storageUsedGB = totalBackupSize / 1024m / 1024m / 1024m; // Convertir bytes a GB
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage usage for tenant {TenantId}", tenantId);
                // En caso de error, asumimos que no hay espacio disponible
                return (false, "Unable to calculate storage usage", 0, maxStorageGB);
            }
        }

        // Verificar si hay espacio disponible (dejamos un margen del 5%)
        var usagePercentage = maxStorageGB > 0 ? (storageUsedGB / maxStorageGB) * 100 : 0;
        
        if (usagePercentage >= 95)
        {
            return (false, $"Storage limit reached. You are using {storageUsedGB:F2} GB of {maxStorageGB} GB. Upgrade your plan for more storage.", storageUsedGB, maxStorageGB);
        }

        return (true, null, storageUsedGB, maxStorageGB);
    }

    public async Task<bool> HasFeatureAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return false;
        }

        // Obtener suscripción activa
        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return false;
        }

        return featureName.ToLower() switch
        {
            "cloudstorageenabled" => subscription.Plan.CloudStorageEnabled,
            "scheduledbackupsenabled" => subscription.Plan.ScheduledBackupsEnabled,
            "apiaccessenabled" => subscription.Plan.ApiAccessEnabled,
            "prioritysupport" => subscription.Plan.PrioritySupport,
            "custombrandingenabled" => subscription.Plan.CustomBrandingEnabled,
            _ => false
        };
    }

    public async Task<string?> GetCurrentPlanNameAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return null;
        }

        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        return subscription?.Plan.DisplayName;
    }

    public async Task<(int MaxDatabases, int MaxUsers, long MaxStorageGB, bool ScheduledBackupsEnabled, bool ApiAccessEnabled)> GetCurrentLimitsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextService.TenantId;
        if (!tenantId.HasValue)
        {
            return (0, 0, 0, false, false);
        }

        var subscription = await GetActiveSubscriptionAsync(tenantId.Value, cancellationToken);
        if (subscription == null)
        {
            return (0, 0, 0, false, false);
        }

        return (
            subscription.Plan.MaxDatabases,
            subscription.Plan.MaxUsers,
            subscription.Plan.MaxStorageGB,
            subscription.Plan.ScheduledBackupsEnabled,
            subscription.Plan.ApiAccessEnabled
        );
    }

    /// <summary>
    /// Obtiene la suscripción activa del tenant
    /// </summary>
    private async Task<Domain.Entities.Subscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _masterContext.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId &&
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing) &&
                       s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
