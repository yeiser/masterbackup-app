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
        // Obtener datos del job desde el JobDataMap (almacenados como string)
        var dataMap = context.JobDetail.JobDataMap;
        var backupScheduleIdString = dataMap.GetString("BackupScheduleId");
        var databaseConnectionIdString = dataMap.GetString("DatabaseConnectionId");
        var tenantIdString = dataMap.GetString("TenantId");
        var cronExpression = dataMap.GetString("CronExpression");

        if (string.IsNullOrEmpty(backupScheduleIdString) || 
            string.IsNullOrEmpty(databaseConnectionIdString) ||
            string.IsNullOrEmpty(tenantIdString))
        {
            _logger.LogError("BackupJob executed with missing parameters. BackupScheduleId: {BackupScheduleId}, DatabaseConnectionId: {DatabaseConnectionId}, TenantId: {TenantId}",
                backupScheduleIdString, databaseConnectionIdString, tenantIdString);
            return;
        }

        var backupScheduleId = Guid.Parse(backupScheduleIdString);
        var databaseConnectionId = Guid.Parse(databaseConnectionIdString);
        var tenantId = Guid.Parse(tenantIdString);

        _logger.LogInformation(
            "Executing scheduled backup job. BackupScheduleId: {BackupScheduleId}, " +
            "DatabaseConnectionId: {DatabaseConnectionId}, TenantId: {TenantId}, " +
            "CronExpression: {CronExpression}, ScheduledFireTime: {ScheduledFireTime}",
            backupScheduleId, databaseConnectionId, tenantId, cronExpression, 
            context.ScheduledFireTimeUtc);

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Obtener la información del tenant desde MasterDbContext
            var tenant = await masterDbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
            {
                _logger.LogError("Tenant {TenantId} not found", tenantId);
                return;
            }

            // Establecer el contexto del tenant
            tenantContext.SetTenant(tenantId, tenant.ConnectionString);

            // Ahora sí podemos obtener el TenantDbContext con el contexto correcto
            var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

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

            // Create BackupHistory record first
            var jobId = Guid.NewGuid();
            var backupHistory = new Domain.Entities.BackupHistory
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                BackupScheduleId = backupScheduleId,
                DatabaseConnectionId = backupSchedule.DatabaseConnectionId,
                Status = Domain.Enums.BackupStatus.Pending,
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            tenantDbContext.BackupHistories.Add(backupHistory);
            await tenantDbContext.SaveChangesAsync();

            _logger.LogInformation("Created BackupHistory {BackupHistoryId} for Job {JobId}", 
                backupHistory.Id, jobId);

            // Extract database name for auto-restore
            var dbName = backupSchedule.DatabaseConnection.Database;
            var targetDbName = backupSchedule.AutoRestore ? $"{dbName}_DW" : null;

            // Create backup job message
            var backupJobMessage = new BackupJobMessage
            {
                JobId = jobId,
                TenantId = tenantId,
                BackupScheduleId = backupScheduleId,
                DatabaseConnection = new DatabaseConnectionInfo
                {
                    DatabaseConnectionId = backupSchedule.DatabaseConnection.Id,
                    Name = backupSchedule.DatabaseConnection.Name,
                    ConnectionString = connectionStringBuilder.ToString(),
                    DatabaseType = backupSchedule.DatabaseConnection.Type.ToString()
                },
                BlobStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
                    ?? configuration["AzureStorage:ConnectionString"] 
                    ?? throw new InvalidOperationException("Azure Storage connection string not configured"),
                ContainerName = $"{configuration["AzureStorage:ContainerPrefix"] ?? "backups"}-{tenantId}",
                BackupFileName = $"{backupSchedule.Name.Replace(" ", "-")}_{DateTime.UtcNow:yyyyMMdd-HHmmss}.backup",
                TimeoutMinutes = backupSchedule.TimeoutMinutes,
                MaxRetries = backupSchedule.MaxRetries,
                CurrentRetry = 0,
                ScheduledTime = context.ScheduledFireTimeUtc?.DateTime ?? DateTime.UtcNow,
                CompressionType = "gzip",
                AutoRestore = backupSchedule.AutoRestore,
                TargetDatabaseName = targetDbName
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
                // Obtener el tenant context y db context nuevamente en el catch
                var tenantContextForError = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var masterDbContextForError = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
                
                var tenantForError = await masterDbContextForError.Tenants
                    .FirstOrDefaultAsync(t => t.Id == tenantId);

                if (tenantForError != null)
                {
                    tenantContextForError.SetTenant(tenantId, tenantForError.ConnectionString);
                    var tenantDbContextForError = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
                    
                    var backupSchedule = await tenantDbContextForError.BackupSchedules
                        .FirstOrDefaultAsync(bs => bs.Id == backupScheduleId);
                    
                    if (backupSchedule != null)
                    {
                        backupSchedule.LastRun = DateTime.UtcNow;
                        // Store error information (could add LastError property to BackupSchedule entity)
                        await tenantDbContextForError.SaveChangesAsync();
                    }
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
