using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Domain.Models;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Infrastructure.Services;

namespace MasterBackup_API.Application.Features.Backups.Commands;

public class ExecuteInstantBackupCommandHandler : IRequestHandler<ExecuteInstantBackupCommand, ExecuteInstantBackupResult>
{
    private readonly TenantDbContext _tenantContext;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly INotificationService _notificationService;
    private readonly ITenantContext _tenantContextService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ExecuteInstantBackupCommandHandler> _logger;

    public ExecuteInstantBackupCommandHandler(
        TenantDbContext tenantContext,
        IMessageQueueService messageQueueService,
        IBlobStorageService blobStorageService,
        INotificationService notificationService,
        ITenantContext tenantContextService,
        IEncryptionService encryptionService,
        ILogger<ExecuteInstantBackupCommandHandler> logger)
    {
        _tenantContext = tenantContext;
        _messageQueueService = messageQueueService;
        _blobStorageService = blobStorageService;
        _notificationService = notificationService;
        _tenantContextService = tenantContextService;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<ExecuteInstantBackupResult> Handle(ExecuteInstantBackupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _tenantContextService.TenantId ?? throw new UnauthorizedAccessException("Tenant ID not found");

            _logger.LogInformation("Executing instant backup for DatabaseConnection {DatabaseConnectionId} in Tenant {TenantId}",
                request.DatabaseConnectionId, tenantId);

            // 1. Validate DatabaseConnection exists and is active
            var dbConnection = await _tenantContext.DatabaseConnections
                .AsNoTracking()
                .FirstOrDefaultAsync(dc => dc.Id == request.DatabaseConnectionId && dc.IsActive, cancellationToken);

            if (dbConnection == null)
            {
                _logger.LogWarning("DatabaseConnection {DatabaseConnectionId} not found or inactive", request.DatabaseConnectionId);
                throw new InvalidOperationException($"Database connection {request.DatabaseConnectionId} not found or is not active");
            }

            // 2. Ensure tenant queue exists in RabbitMQ
            await _messageQueueService.CreateTenantQueueAsync(tenantId);

            // 3. Ensure blob storage container exists
            await _blobStorageService.EnsureContainerExistsAsync(tenantId);

            // 4. Create BackupHistory record with Pending status
            var jobId = Guid.NewGuid();
            var backupHistory = new BackupHistory
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                DatabaseConnectionId = request.DatabaseConnectionId,
                BackupScheduleId = null, // Instant backup - no schedule
                Status = BackupStatus.Pending,
                StartTime = DateTime.UtcNow,
                IsInstantBackup = true,
                CompressionType = request.CompressionType ?? "GZIP",
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _tenantContext.BackupHistories.Add(backupHistory);
            await _tenantContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created BackupHistory record {BackupHistoryId} for Job {JobId}",
                backupHistory.Id, jobId);

            // 5. Decrypt database password for worker
            var decryptedPassword = _encryptionService.Decrypt(dbConnection.EncryptedPassword);

            // 6. Build connection string for worker
            var connectionString = BuildConnectionString(dbConnection, decryptedPassword);

            // 7. Generate blob file name
            var blobFileName = GenerateBlobFileName(dbConnection, jobId);

            // 8. Get blob storage connection string from configuration
            var blobConnectionString = _blobStorageService.GetType()
                .GetProperty("ConnectionString")?.GetValue(_blobStorageService)?.ToString()
                ?? Environment.GetEnvironmentVariable("AzureStorage__ConnectionString")
                ?? "UseDevelopmentStorage=true";

            // 9. Build BackupJobMessage for RabbitMQ
            var backupJobMessage = new BackupJobMessage
            {
                JobId = jobId,
                TenantId = tenantId,
                BackupScheduleId = Guid.Empty, // Instant backup - using Empty instead of null
                DatabaseConnection = new DatabaseConnectionInfo
                {
                    DatabaseConnectionId = dbConnection.Id,
                    Name = dbConnection.Name,
                    ConnectionString = connectionString,
                    DatabaseType = dbConnection.Type.ToString()
                },
                BlobStorageConnectionString = blobConnectionString,
                ContainerName = $"backups-{tenantId.ToString().ToLowerInvariant()}",
                BackupFileName = blobFileName,
                TimeoutMinutes = request.TimeoutMinutes ?? 30,
                MaxRetries = request.MaxRetries ?? 3,
                CurrentRetry = 0,
                ScheduledTime = DateTime.UtcNow,
                CompressionType = request.CompressionType ?? "GZIP",
                EncryptionKey = null // Optional: implement encryption key if needed
            };

            // 10. Publish message to RabbitMQ
            await _messageQueueService.PublishBackupJobAsync(tenantId, backupJobMessage);

            _logger.LogInformation("Published backup job {JobId} to RabbitMQ for Tenant {TenantId}",
                jobId, tenantId);

            // 11. Send SignalR notification to tenant
            try
            {
                var notification = new Application.Common.DTOs.BackupStartedDto
                {
                    JobId = jobId,
                    TenantId = tenantId,
                    BackupScheduleId = Guid.Empty,
                    Timestamp = DateTime.UtcNow,
                    DatabaseType = dbConnection.Type.ToString(),
                    EstimatedDurationMinutes = request.TimeoutMinutes ?? 30
                };

                await _notificationService.NotifyBackupStartedAsync(tenantId, notification);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send SignalR notification for Job {JobId}", jobId);
                // Don't fail the operation if notification fails
            }

            return new ExecuteInstantBackupResult
            {
                BackupHistoryId = backupHistory.Id,
                JobId = jobId,
                Message = "Backup job queued successfully",
                QueuedAt = DateTime.UtcNow
            };

        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }

    private string BuildConnectionString(DatabaseConnection dbConnection, string decryptedPassword)
    {
        return dbConnection.Type switch
        {
            DatabaseType.PostgreSQL => $"Host={dbConnection.Host};Port={dbConnection.Port};Database={dbConnection.Database};Username={dbConnection.Username};Password={decryptedPassword};SSL Mode={dbConnection.SSLMode ?? "Prefer"}",
            DatabaseType.MySQL => $"Server={dbConnection.Host};Port={dbConnection.Port};Database={dbConnection.Database};Uid={dbConnection.Username};Pwd={decryptedPassword};SslMode={dbConnection.SSLMode ?? "Preferred"}",
            DatabaseType.SQLServer => $"Server={dbConnection.Host},{dbConnection.Port};Database={dbConnection.Database};User Id={dbConnection.Username};Password={decryptedPassword};TrustServerCertificate=True",
            DatabaseType.MongoDB => $"mongodb://{dbConnection.Username}:{decryptedPassword}@{dbConnection.Host}:{dbConnection.Port}/{dbConnection.Database}",
            _ => throw new NotSupportedException($"Database type {dbConnection.Type} is not supported")
        };
    }

    private string GenerateBlobFileName(DatabaseConnection dbConnection, Guid jobId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dbName = dbConnection.Database.Replace(" ", "_");
        var dbType = dbConnection.Type.ToString().ToLower();

        return $"instant_{dbType}_{dbName}_{timestamp}_{jobId:N}.sql.gz";
    }
}
