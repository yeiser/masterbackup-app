using Quartz;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Infrastructure.Services;
using MasterBackup_API.Domain.Models;
using Npgsql;

namespace MasterBackup_API.Infrastructure.Jobs;

/// <summary>
/// Job de Quartz.NET que ejecuta backups programados
/// Este job se dispara según la expresión CRON configurada y envía un mensaje a RabbitMQ
/// </summary>
[DisallowConcurrentExecution] // Previene ejecuciones concurrentes del mismo job
public class BackupJob : IJob
{
    private readonly ILogger<BackupJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BackupJob(ILogger<BackupJob> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Método ejecutado por Quartz.NET cuando el job se dispara
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        // Obtener datos del job desde el JobDataMap
        var dataMap = context.JobDetail.JobDataMap;
        var backupScheduleId = dataMap.GetGuid("BackupScheduleId");
        var databaseConnectionId = dataMap.GetGuid("DatabaseConnectionId");
        var tenantId = dataMap.GetGuid("TenantId");
        var cronExpression = dataMap.GetString("CronExpression");

        _logger.LogInformation(
            "Executing scheduled backup job. BackupScheduleId: {BackupScheduleId}, " +
            "DatabaseConnectionId: {DatabaseConnectionId}, TenantId: {TenantId}, " +
            "CronExpression: {CronExpression}, ScheduledFireTime: {ScheduledFireTime}",
            backupScheduleId, databaseConnectionId, tenantId, cronExpression, 
            context.ScheduledFireTimeUtc);

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Get BackupSchedule and DatabaseConnection details
            var backupSchedule = await tenantDbContext.BackupSchedules
                .Include(bs => bs.DatabaseConnection)
                .FirstOrDefaultAsync(bs => bs.Id == backupScheduleId);

            if (backupSchedule == null)
            {
                _logger.LogWarning("BackupSchedule {BackupScheduleId} not found", backupScheduleId);
                return;
            }

            if (backupSchedule.DatabaseConnection == null)
            {
                _logger.LogWarning("DatabaseConnection not found for BackupSchedule {BackupScheduleId}", backupScheduleId);
                return;
            }

            // Decrypt password and build connection string
            var decryptedPassword = encryptionService.Decrypt(backupSchedule.DatabaseConnection.EncryptedPassword);
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = backupSchedule.DatabaseConnection.Host,
                Port = backupSchedule.DatabaseConnection.Port,
                Database = backupSchedule.DatabaseConnection.Database,
                Username = backupSchedule.DatabaseConnection.Username,
                Password = decryptedPassword
            };

            // Create backup job message
            var backupJobMessage = new BackupJobMessage
            {
                JobId = Guid.NewGuid(),
                TenantId = tenantId,
                BackupScheduleId = backupScheduleId,
                DatabaseConnection = new DatabaseConnectionInfo
                {
                    DatabaseConnectionId = backupSchedule.DatabaseConnection.Id,
                    Name = backupSchedule.DatabaseConnection.Name,
                    ConnectionString = connectionStringBuilder.ToString(),
                    DatabaseType = backupSchedule.DatabaseConnection.Type.ToString()
                },
                BlobStorageConnectionString = configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true",
                ContainerName = $"{configuration["AzureStorage:ContainerPrefix"] ?? "backups"}-{tenantId}",
                BackupFileName = $"{backupSchedule.Name.Replace(" ", "-")}_{DateTime.UtcNow:yyyyMMdd-HHmmss}.backup",
                TimeoutMinutes = backupSchedule.TimeoutMinutes,
                MaxRetries = backupSchedule.MaxRetries,
                CurrentRetry = 0,
                ScheduledTime = context.ScheduledFireTimeUtc?.DateTime ?? DateTime.UtcNow,
                CompressionType = "gzip"
            };

            // Ensure tenant queue exists
            await messageQueueService.CreateTenantQueueAsync(tenantId);

            // Publish message to RabbitMQ
            await messageQueueService.PublishBackupJobAsync(tenantId, backupJobMessage);

            // Update LastRun and NextRun in BackupSchedule
            backupSchedule.LastRun = DateTime.UtcNow;
            backupSchedule.NextRun = context.NextFireTimeUtc?.DateTime;
            await tenantDbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Backup job {JobId} published successfully for BackupSchedule {BackupScheduleId}",
                backupJobMessage.JobId, backupScheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error executing backup job. BackupScheduleId: {BackupScheduleId}",
                backupScheduleId);

            // Update LastExecutionStatus as Failed
            try
            {
                var backupSchedule = await tenantDbContext.BackupSchedules
                    .FirstOrDefaultAsync(bs => bs.Id == backupScheduleId);
                
                if (backupSchedule != null)
                {
                    backupSchedule.LastRun = DateTime.UtcNow;
                    // Store error information (could add LastError property to BackupSchedule entity)
                    await tenantDbContext.SaveChangesAsync();
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error updating BackupSchedule after job failure");
            }
            
            // Propagar la excepción para que Quartz.NET pueda manejar los reintentos
            throw;
        }
    }
}

/// <summary>
/// Extensiones helper para JobDataMap
/// </summary>
public static class JobDataMapExtensions
{
    public static Guid GetGuid(this JobDataMap dataMap, string key)
    {
        var value = dataMap.GetString(key);
        return Guid.Parse(value ?? throw new ArgumentException($"Key '{key}' not found in JobDataMap"));
    }
}
