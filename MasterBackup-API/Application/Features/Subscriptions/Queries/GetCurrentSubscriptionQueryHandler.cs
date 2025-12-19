using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.Subscriptions.Queries;

public class GetCurrentSubscriptionQueryHandler : IRequestHandler<GetCurrentSubscriptionQuery, SubscriptionDto?>
{
    private readonly MasterDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCurrentSubscriptionQueryHandler(MasterDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<SubscriptionDto?> Handle(GetCurrentSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return null;
        }

        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId.Value && 
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            return null;
        }

        // Obtener uso actual del tenant usando conexión directa
        int databasesCount = 0;
        decimal storageUsedGB = 0;

        if (!string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            try
            {
                using (var connection = new Npgsql.NpgsqlConnection(_tenantContext.ConnectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    // Contar bases de datos registradas
                    using (var cmd = new Npgsql.NpgsqlCommand(
                        @"SELECT COUNT(*) FROM ""DatabaseConnections""", connection))
                    {
                        var result = await cmd.ExecuteScalarAsync(cancellationToken);
                        databasesCount = result != null ? Convert.ToInt32(result) : 0;
                    }

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
            catch (Exception)
            {
                // Si hay error al conectar a la BD del tenant, dejar valores en 0
            }
        }

        // Contar usuarios del tenant
        var usersCount = await _context.Users
            .Where(u => u.TenantId == tenantId.Value)
            .CountAsync(cancellationToken);

        // Calcular porcentaje de almacenamiento
        var maxStorageGB = subscription.Plan.MaxStorageGB;
        var storagePercentage = maxStorageGB > 0 
            ? (int)Math.Min(100, (storageUsedGB / maxStorageGB) * 100)
            : 0;

        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan.Name,
            PlanDisplayName = subscription.Plan.DisplayName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status.ToString(),
            BillingCycle = subscription.BillingCycle.ToString(),
            Amount = subscription.Amount,
            Currency = subscription.Currency,
            AutoRenew = subscription.AutoRenew,
            CanceledAt = subscription.CanceledAt,
            CancellationReason = subscription.CancellationReason,
            TrialEndDate = subscription.TrialEndDate,
            DaysRemaining = (int)(subscription.EndDate - DateTime.UtcNow).TotalDays,
            Limits = new PlanLimitsDto
            {
                MaxDatabases = subscription.Plan.MaxDatabases,
                MaxUsers = subscription.Plan.MaxUsers,
                MaxStorageGB = subscription.Plan.MaxStorageGB,
                BackupRetentionDays = subscription.Plan.BackupRetentionDays,
                CloudStorageEnabled = subscription.Plan.CloudStorageEnabled,
                ScheduledBackupsEnabled = subscription.Plan.ScheduledBackupsEnabled,
                ApiAccessEnabled = subscription.Plan.ApiAccessEnabled,
                PrioritySupport = subscription.Plan.PrioritySupport,
                CustomBrandingEnabled = subscription.Plan.CustomBrandingEnabled
            },
            Usage = new CurrentUsageDto
            {
                DatabasesCount = databasesCount,
                UsersCount = usersCount,
                StorageUsedGB = storageUsedGB,
                StoragePercentage = storagePercentage
            }
        };
    }
}
