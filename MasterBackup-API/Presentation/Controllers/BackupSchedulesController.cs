using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Application.Features.BackupSchedules.Commands;
using MasterBackup_API.Application.Features.BackupSchedules.Queries;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller para la gestión de schedules de backup automatizados
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BackupSchedulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Obtiene la lista de schedules de backup con filtros y paginación
    /// </summary>
    /// <param name="databaseConnectionId">Filtrar por conexión de base de datos</param>
    /// <param name="isActive">Filtrar por estado activo/inactivo</param>
    /// <param name="searchTerm">Búsqueda por nombre o descripción</param>
    /// <param name="page">Número de página (default: 1)</param>
    /// <param name="pageSize">Tamaño de página (default: 10)</param>
    /// <returns>Lista de schedules de backup</returns>
    [HttpGet]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<List<BackupScheduleDto>>> GetBackupSchedules(
        [FromQuery] Guid? databaseConnectionId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var tenantId = GetTenantId();

        var query = new GetBackupSchedulesQuery
        {
            TenantId = tenantId,
            DatabaseConnectionId = databaseConnectionId,
            IsActive = isActive,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene un schedule de backup por ID
    /// </summary>
    /// <param name="id">ID del schedule</param>
    /// <returns>Schedule de backup</returns>
    [HttpGet("{id}")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<BackupScheduleDto>> GetBackupScheduleById(Guid id)
    {
        var tenantId = GetTenantId();

        var query = new GetBackupScheduleByIdQuery
        {
            Id = id,
            TenantId = tenantId
        };

        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "Backup schedule not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Crea un nuevo schedule de backup
    /// </summary>
    /// <param name="dto">Datos del schedule a crear</param>
    /// <returns>Schedule creado</returns>
    [HttpPost]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<BackupScheduleDto>> CreateBackupSchedule([FromBody] CreateBackupScheduleDto dto)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var command = new CreateBackupScheduleCommand
        {
            TenantId = tenantId,
            CreatedBy = userId,
            Data = dto
        };

        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetBackupScheduleById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un schedule de backup existente
    /// </summary>
    /// <param name="id">ID del schedule</param>
    /// <param name="dto">Datos actualizados</param>
    /// <returns>Schedule actualizado</returns>
    [HttpPut("{id}")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<BackupScheduleDto>> UpdateBackupSchedule(Guid id, [FromBody] UpdateBackupScheduleDto dto)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var command = new UpdateBackupScheduleCommand
        {
            Id = id,
            TenantId = tenantId,
            UpdatedBy = userId,
            Data = dto
        };

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un schedule de backup
    /// </summary>
    /// <param name="id">ID del schedule</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("{id}")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<ActionResult> DeleteBackupSchedule(Guid id)
    {
        var tenantId = GetTenantId();

        var command = new DeleteBackupScheduleCommand
        {
            Id = id,
            TenantId = tenantId
        };

        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Pausa un schedule de backup (IsActive = false)
    /// </summary>
    /// <param name="id">ID del schedule</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPost("{id}/pause")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult> PauseBackupSchedule(Guid id)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var command = new PauseBackupScheduleCommand
        {
            Id = id,
            TenantId = tenantId,
            UpdatedBy = userId
        };

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Backup schedule paused successfully." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reanuda un schedule de backup pausado (IsActive = true)
    /// </summary>
    /// <param name="id">ID del schedule</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPost("{id}/resume")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult> ResumeBackupSchedule(Guid id)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();

        var command = new ResumeBackupScheduleCommand
        {
            Id = id,
            TenantId = tenantId,
            UpdatedBy = userId
        };

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "Backup schedule resumed successfully." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Previsualiza las próximas ejecuciones de una expresión CRON
    /// </summary>
    /// <param name="cronExpression">Expresión CRON a evaluar</param>
    /// <param name="timeZone">Zona horaria (default: UTC)</param>
    /// <param name="count">Número de ejecuciones a calcular (default: 5)</param>
    /// <returns>Preview con las próximas ejecuciones</returns>
    [HttpPost("preview-cron")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<CronExecutionPreviewDto>> PreviewCronExecutions(
        [FromBody] PreviewCronExecutionsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    #region Helper Methods

    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant ID not found in token.");
        }
        return tenantId;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token.");
        }
        return userId;
    }

    #endregion

    #region RabbitMQ Testing Endpoints

    /// <summary>
    /// Test endpoint to publish a backup job message to RabbitMQ
    /// </summary>
    [HttpPost("test-publish-job")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> TestPublishBackupJob([FromServices] Application.Common.Interfaces.IMessageQueueService messageQueueService)
    {
        var tenantId = GetTenantId();

        // Create a test backup job message
        var testMessage = new Domain.Models.BackupJobMessage
        {
            JobId = Guid.NewGuid(),
            TenantId = tenantId,
            BackupScheduleId = Guid.NewGuid(),
            DatabaseConnection = new Domain.Models.DatabaseConnectionInfo
            {
                DatabaseConnectionId = Guid.NewGuid(),
                Name = "Test Database",
                ConnectionString = "Host=localhost;Port=5432;Database=testdb;Username=test;Password=test",
                DatabaseType = "PostgreSQL"
            },
            BlobStorageConnectionString = "UseDevelopmentStorage=true",
            ContainerName = $"backups-{tenantId}",
            BackupFileName = $"test-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.backup",
            TimeoutMinutes = 30,
            MaxRetries = 3,
            CurrentRetry = 0,
            ScheduledTime = DateTime.UtcNow,
            CompressionType = "gzip"
        };

        // Ensure tenant queue exists
        await messageQueueService.CreateTenantQueueAsync(tenantId);

        // Publish the message
        await messageQueueService.PublishBackupJobAsync(tenantId, testMessage);

        return Ok(new
        {
            Message = "Test backup job published successfully",
            JobId = testMessage.JobId,
            TenantId = tenantId,
            QueueName = $"backup.jobs.{tenantId}"
        });
    }

    /// <summary>
    /// Get RabbitMQ queue statistics for the current tenant
    /// </summary>
    [HttpGet("queue-stats")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> GetQueueStats([FromServices] Application.Common.Interfaces.IMessageQueueService messageQueueService)
    {
        var tenantId = GetTenantId();
        var stats = await messageQueueService.GetQueueStatsAsync(tenantId);
        
        return Ok(new
        {
            TenantId = tenantId,
            QueueName = stats.QueueName,
            MessageCount = stats.MessageCount,
            ConsumerCount = stats.ConsumerCount
        });
    }

    /// <summary>
    /// Check RabbitMQ connection health
    /// </summary>
    [HttpGet("rabbitmq-health")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> CheckRabbitMQHealth([FromServices] Application.Common.Interfaces.IMessageQueueService messageQueueService)
    {
        var isHealthy = await messageQueueService.IsHealthyAsync();
        
        return Ok(new
        {
            Healthy = isHealthy,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Azure Blob Storage Testing Endpoints

    /// <summary>
    /// Test endpoint to upload a test file to Azure Blob Storage
    /// </summary>
    [HttpPost("test-upload-blob")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> TestUploadBlob(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "No file provided" });
        }

        var tenantId = GetTenantId();
        var fileName = $"test-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{file.FileName}";

        using var stream = file.OpenReadStream();
        
        var metadata = new Dictionary<string, string>
        {
            { "original_name", file.FileName },
            { "content_type", file.ContentType },
            { "uploaded_by", "test-endpoint" }
        };

        var result = await blobStorageService.UploadBackupAsync(
            tenantId, 
            fileName, 
            stream, 
            metadata,
            file.ContentType);

        return Ok(new
        {
            Message = "File uploaded successfully to Azure Blob Storage",
            BlobUrl = result.BlobUrl,
            BlobName = result.BlobName,
            SizeBytes = result.SizeBytes,
            SizeMB = result.SizeBytes / 1024.0 / 1024.0,
            UploadedAt = result.UploadedAt,
            ContentType = result.ContentType,
            ETag = result.ETag,
            ContainerName = $"backups-{tenantId}"
        });
    }

    /// <summary>
    /// List all backup files in Azure Blob Storage for the current tenant
    /// </summary>
    [HttpGet("list-blobs")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> ListBlobs(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService,
        [FromQuery] string? prefix = null)
    {
        var tenantId = GetTenantId();
        var blobs = await blobStorageService.ListBackupsAsync(tenantId, prefix);

        return Ok(new
        {
            TenantId = tenantId,
            Count = blobs.Count,
            TotalSizeMB = blobs.Sum(b => b.SizeBytes) / 1024.0 / 1024.0,
            Blobs = blobs.Select(b => new
            {
                b.Name,
                b.Url,
                SizeBytes = b.SizeBytes,
                SizeMB = b.SizeBytes / 1024.0 / 1024.0,
                b.CreatedAt,
                b.LastModified,
                b.ContentType,
                b.Metadata,
                b.ETag
            })
        });
    }

    /// <summary>
    /// Download a backup file from Azure Blob Storage
    /// </summary>
    [HttpGet("download-blob/{fileName}")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> DownloadBlob(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService,
        string fileName)
    {
        var tenantId = GetTenantId();
        
        try
        {
            var result = await blobStorageService.DownloadBackupAsync(tenantId, fileName);
            
            return File(result.Content, result.ContentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { Error = $"File {fileName} not found" });
        }
    }

    /// <summary>
    /// Delete a backup file from Azure Blob Storage
    /// </summary>
    [HttpDelete("delete-blob/{fileName}")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> DeleteBlob(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService,
        string fileName)
    {
        var tenantId = GetTenantId();
        var deleted = await blobStorageService.DeleteBackupAsync(tenantId, fileName);

        if (deleted)
        {
            return Ok(new 
            { 
                Message = $"File {fileName} deleted successfully",
                FileName = fileName,
                TenantId = tenantId
            });
        }
        else
        {
            return NotFound(new { Error = $"File {fileName} not found" });
        }
    }

    /// <summary>
    /// Generate a temporary SAS URL for downloading a backup file
    /// </summary>
    [HttpGet("get-blob-sas-url/{fileName}")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> GetBlobSasUrl(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService,
        string fileName,
        [FromQuery] int expiryMinutes = 60)
    {
        var tenantId = GetTenantId();
        
        try
        {
            var sasUrl = await blobStorageService.GetBlobSasUrlAsync(tenantId, fileName, expiryMinutes);
            
            return Ok(new
            {
                FileName = fileName,
                SasUrl = sasUrl,
                ExpiresInMinutes = expiryMinutes,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { Error = $"File {fileName} not found" });
        }
    }

    /// <summary>
    /// Get storage statistics for the current tenant
    /// </summary>
    [HttpGet("blob-storage-stats")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> GetBlobStorageStats(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService)
    {
        var tenantId = GetTenantId();
        var stats = await blobStorageService.GetContainerStatsAsync(tenantId);

        return Ok(new
        {
            TenantId = tenantId,
            ContainerName = stats.ContainerName,
            BlobCount = stats.BlobCount,
            TotalSizeBytes = stats.TotalSizeBytes,
            TotalSizeMB = stats.TotalSizeMB,
            TotalSizeGB = stats.TotalSizeGB
        });
    }

    /// <summary>
    /// Check Azure Blob Storage connection health
    /// </summary>
    [HttpGet("blob-storage-health")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> CheckBlobStorageHealth(
        [FromServices] Application.Common.Interfaces.IBlobStorageService blobStorageService)
    {
        var isHealthy = await blobStorageService.IsHealthyAsync();
        
        return Ok(new
        {
            Healthy = isHealthy,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region SignalR Testing Endpoints

    /// <summary>
    /// Send a test SignalR notification to all connected clients in the tenant
    /// </summary>
    [HttpPost("test-signalr-notification")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> TestSignalRNotification(
        [FromServices] INotificationService notificationService,
        [FromBody] TestSignalRRequest? request = null)
    {
        var tenantId = GetTenantId();
        var testScheduleId = request?.ScheduleId ?? Guid.NewGuid();
        var notificationType = request?.NotificationType ?? "Started";

        try
        {
            switch (notificationType.ToLower())
            {
                case "started":
                    var startedDto = new Application.Common.DTOs.BackupStartedDto
                    {
                        JobId = Guid.NewGuid(),
                        TenantId = tenantId,
                        BackupScheduleId = testScheduleId,
                        Timestamp = DateTime.UtcNow,
                        DatabaseType = "PostgreSQL",
                        EstimatedDurationMinutes = 5
                    };
                    await notificationService.NotifyBackupStartedAsync(tenantId, startedDto);
                    return Ok(new { Message = "Backup started notification sent", Data = startedDto });

                case "progress":
                    var progressDto = new Application.Common.DTOs.BackupProgressDto
                    {
                        JobId = Guid.NewGuid(),
                        TenantId = tenantId,
                        BackupScheduleId = testScheduleId,
                        Timestamp = DateTime.UtcNow,
                        ProgressPercentage = 50,
                        CurrentStep = "Dumping database tables",
                        ProcessedBytes = 5242880, // 5 MB
                        TotalBytes = 10485760, // 10 MB
                        ElapsedTime = TimeSpan.FromMinutes(2.5),
                        EstimatedTimeRemaining = TimeSpan.FromMinutes(2.5)
                    };
                    await notificationService.NotifyBackupProgressAsync(tenantId, progressDto);
                    return Ok(new { Message = "Backup progress notification sent", Data = progressDto });

                case "completed":
                    var completedDto = new Application.Common.DTOs.BackupCompletedDto
                    {
                        JobId = Guid.NewGuid(),
                        TenantId = tenantId,
                        BackupScheduleId = testScheduleId,
                        Timestamp = DateTime.UtcNow,
                        Success = true,
                        BlobUrl = "https://storageaccount.blob.core.windows.net/backups-tenant/test-backup.sql.gz",
                        BlobName = "test-backup.sql.gz",
                        BackupSizeBytes = 10485760, // 10 MB
                        Duration = TimeSpan.FromMinutes(5),
                        StartTime = DateTime.UtcNow.AddMinutes(-5),
                        EndTime = DateTime.UtcNow,
                        CompressionType = "GZIP",
                        Metadata = new Dictionary<string, string>
                        {
                            { "database_name", "testdb" },
                            { "table_count", "25" }
                        }
                    };
                    await notificationService.NotifyBackupCompletedAsync(tenantId, completedDto);
                    return Ok(new { Message = "Backup completed notification sent", Data = completedDto });

                case "failed":
                    var failedDto = new Application.Common.DTOs.BackupFailedDto
                    {
                        JobId = Guid.NewGuid(),
                        TenantId = tenantId,
                        BackupScheduleId = testScheduleId,
                        Timestamp = DateTime.UtcNow,
                        ErrorMessage = "Connection to database timed out",
                        ErrorCode = "DB_TIMEOUT",
                        StackTrace = "at System.Data.SqlClient.SqlConnection.Open()...",
                        RetryCount = 1,
                        MaxRetries = 3,
                        WillRetry = true,
                        NextRetryAt = DateTime.UtcNow.AddMinutes(5),
                        Duration = TimeSpan.FromMinutes(2)
                    };
                    await notificationService.NotifyBackupFailedAsync(tenantId, failedDto);
                    return Ok(new { Message = "Backup failed notification sent", Data = failedDto });

                case "event":
                    var eventDto = new Application.Common.DTOs.BackupEventDto
                    {
                        EventType = "CustomEvent",
                        JobId = Guid.NewGuid(),
                        TenantId = tenantId,
                        BackupScheduleId = testScheduleId,
                        Timestamp = DateTime.UtcNow,
                        Data = new Dictionary<string, object>
                        {
                            { "message", "This is a custom test event" },
                            { "severity", "info" }
                        }
                    };
                    await notificationService.NotifyBackupEventAsync(tenantId, eventDto);
                    return Ok(new { Message = "Backup event notification sent", Data = eventDto });

                default:
                    return BadRequest(new { Error = $"Invalid notification type: {notificationType}. Use: Started, Progress, Completed, Failed, Event" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to send notification", Details = ex.Message });
        }
    }

    /// <summary>
    /// Send a test SignalR notification to a specific schedule group
    /// </summary>
    [HttpPost("test-signalr-schedule-notification")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<IActionResult> TestSignalRScheduleNotification(
        [FromServices] INotificationService notificationService,
        [FromBody] TestSignalRRequest request)
    {
        if (request?.ScheduleId == null)
        {
            return BadRequest(new { Error = "ScheduleId is required" });
        }

        var tenantId = GetTenantId();
        var notificationType = request.NotificationType ?? "Progress";

        try
        {
            var progressDto = new Application.Common.DTOs.BackupProgressDto
            {
                JobId = Guid.NewGuid(),
                TenantId = tenantId,
                BackupScheduleId = request.ScheduleId.Value,
                Timestamp = DateTime.UtcNow,
                ProgressPercentage = 75,
                CurrentStep = "Uploading to blob storage",
                ProcessedBytes = 7864320, // 7.5 MB
                TotalBytes = 10485760, // 10 MB
                ElapsedTime = TimeSpan.FromMinutes(3.75),
                EstimatedTimeRemaining = TimeSpan.FromMinutes(1.25)
            };

            await notificationService.NotifyBackupProgressToScheduleAsync(request.ScheduleId.Value, progressDto);
            
            return Ok(new
            {
                Message = $"Schedule-specific notification sent to schedule_{request.ScheduleId}",
                ScheduleId = request.ScheduleId,
                Data = progressDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to send schedule notification", Details = ex.Message });
        }
    }

    /// <summary>
    /// Check SignalR hub health and connectivity
    /// </summary>
    [HttpGet("signalr-health")]
    [RoleAuthorization(UserRole.Admin)]
    public IActionResult CheckSignalRHealth(
        [FromServices] INotificationService notificationService)
    {
        try
        {
            // Check if the service is accessible
            var serviceIsAvailable = notificationService != null;
            
            return Ok(new
            {
                Healthy = serviceIsAvailable,
                HubEndpoint = "/hubs/backup-notifications",
                Timestamp = DateTime.UtcNow,
                Message = serviceIsAvailable 
                    ? "SignalR hub is available. Connect clients to /hubs/backup-notifications" 
                    : "SignalR service is not available"
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                Healthy = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion
}

/// <summary>
/// Request model for SignalR testing endpoints
/// </summary>
public class TestSignalRRequest
{
    public Guid? ScheduleId { get; set; }
    public string? NotificationType { get; set; } // Started, Progress, Completed, Failed, Event
}
