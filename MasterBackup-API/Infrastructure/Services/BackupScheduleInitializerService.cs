using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// Background service que carga todos los BackupSchedules activos al iniciar la aplicación
/// y los registra en Quartz.NET
/// </summary>
public class BackupScheduleInitializerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackupScheduleInitializerService> _logger;

    public BackupScheduleInitializerService(
        IServiceProvider serviceProvider,
        ILogger<BackupScheduleInitializerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Esperar un poco para asegurar que Quartz esté completamente iniciado
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInformation("Loading existing backup schedules into Quartz...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var schedulerService = scope.ServiceProvider.GetRequiredService<IBackupSchedulerService>();

            // Obtener todos los tenants activos
            var tenants = await masterDbContext.Tenants
                .Where(t => t.IsActive)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Found {TenantCount} active tenants", tenants.Count);

            int totalSchedulesLoaded = 0;

            foreach (var tenant in tenants)
            {
                try
                {
                    // Crear un scope para cada tenant
                    using var tenantScope = _serviceProvider.CreateScope();
                    var tenantContext = tenantScope.ServiceProvider.GetRequiredService<ITenantContext>();
                    
                    // Establecer el contexto del tenant
                    tenantContext.SetTenant(tenant.Id, tenant.ConnectionString);

                    var tenantDbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();

                    // Obtener todos los schedules activos de este tenant
                    var schedules = await tenantDbContext.BackupSchedules
                        .Include(s => s.DatabaseConnection)
                        .Where(s => s.IsActive)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation("Tenant {TenantId}: Found {ScheduleCount} active schedules", 
                        tenant.Id, schedules.Count);

                    foreach (var schedule in schedules)
                    {
                        try
                        {
                            await schedulerService.ScheduleBackupAsync(schedule, stoppingToken);
                            totalSchedulesLoaded++;
                            
                            _logger.LogInformation(
                                "Loaded schedule {ScheduleId} ({ScheduleName}) - Next run: {NextRun}",
                                schedule.Id, schedule.Name, schedule.NextRun);
                        }
                        catch (Exception scheduleEx)
                        {
                            _logger.LogError(scheduleEx, 
                                "Error loading schedule {ScheduleId} ({ScheduleName})", 
                                schedule.Id, schedule.Name);
                        }
                    }

                    // Limpiar el contexto del tenant
                    tenantContext.Clear();
                }
                catch (Exception tenantEx)
                {
                    _logger.LogError(tenantEx, 
                        "Error loading schedules for tenant {TenantId}", 
                        tenant.Id);
                }
            }

            _logger.LogInformation(
                "✅ Backup schedule initialization completed. Loaded {ScheduleCount} schedules from {TenantCount} tenants",
                totalSchedulesLoaded, tenants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing backup schedules");
        }
    }
}
