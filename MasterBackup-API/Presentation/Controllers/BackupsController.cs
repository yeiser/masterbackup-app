using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Features.Backups.Commands;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;
using System.Security.Claims;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller for backup execution and history management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BackupsController> _logger;

    public BackupsController(IMediator mediator, ILogger<BackupsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Execute an instant (manual) backup for a database connection
    /// </summary>
    [HttpPost("execute-instant")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<IActionResult> ExecuteInstantBackup([FromBody] ExecuteInstantBackupRequest request)
    {
        try
        {
            var command = new ExecuteInstantBackupCommand
            {
                DatabaseConnectionId = request.DatabaseConnectionId,
                CompressionType = request.CompressionType ?? "GZIP",
                TimeoutMinutes = request.TimeoutMinutes,
                MaxRetries = request.MaxRetries
            };

            var result = await _mediator.Send(command);

            return Ok(new
            {
                Success = true,
                Data = result,
                Message = "Backup job queued successfully. You will receive real-time notifications via SignalR."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when executing instant backup");
            return BadRequest(new { Success = false, Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing instant backup");
            return StatusCode(500, new { Success = false, Error = "Failed to queue backup job" });
        }
    }

    /// <summary>
    /// Get paginated backup history with optional filters
    /// </summary>
    [HttpGet("history")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<IActionResult> GetBackupHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? backupScheduleId = null,
        [FromQuery] Guid? databaseConnectionId = null,
        [FromQuery] BackupStatus? status = null,
        [FromQuery] bool? isInstantBackup = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] string sortBy = "StartTime",
        [FromQuery] string sortDirection = "DESC")
    {
        try
        {
            var query = new Application.Features.Backups.Queries.GetBackupHistoryQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                BackupScheduleId = backupScheduleId,
                DatabaseConnectionId = databaseConnectionId,
                Status = status,
                IsInstantBackup = isInstantBackup,
                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            var result = await _mediator.Send(query);

            return Ok(new
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching backup history");
            return StatusCode(500, new { Success = false, Error = "Failed to fetch backup history" });
        }
    }

    /// <summary>
    /// Get a single backup history record by ID
    /// </summary>
    [HttpGet("history/{id}")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<IActionResult> GetBackupHistoryById(Guid id)
    {
        try
        {
            var query = new Application.Features.Backups.Queries.GetBackupHistoryByIdQuery
            {
                Id = id
            };

            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { Success = false, Error = "Backup history not found" });
            }

            return Ok(new
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching backup history by ID");
            return StatusCode(500, new { Success = false, Error = "Failed to fetch backup history" });
        }
    }

    /// <summary>
    /// Update backup status from Worker (webhook endpoint)
    /// This endpoint is called by Workers to report backup progress and results
    /// </summary>
    [HttpPost("update-status")]
    [Authorize] // Worker must authenticate via JWT or API key
    public async Task<IActionResult> UpdateBackupStatus([FromBody] UpdateBackupStatusRequest request)
    {
        try
        {
            var command = new UpdateBackupStatusCommand
            {
                JobId = request.JobId,
                TenantId = request.TenantId,
                Status = request.Status,
                ProgressPercentage = request.ProgressPercentage,
                CurrentStep = request.CurrentStep,
                ProcessedBytes = request.ProcessedBytes,
                TotalBytes = request.TotalBytes,
                BlobUrl = request.BlobUrl,
                BlobName = request.BlobName,
                BackupSizeBytes = request.BackupSizeBytes,
                CompressionType = request.CompressionType,
                Metadata = request.Metadata,
                ErrorMessage = request.ErrorMessage,
                ErrorCode = request.ErrorCode,
                StackTrace = request.StackTrace,
                RetryCount = request.RetryCount,
                WillRetry = request.WillRetry,
                NextRetryAt = request.NextRetryAt
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(new { Success = false, Error = result.Message });
            }

            return Ok(new
            {
                Success = true,
                Message = result.Message,
                BackupHistoryId = result.BackupHistoryId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup status for Job {JobId}", request.JobId);
            return StatusCode(500, new { Success = false, Error = "Failed to update backup status" });
        }
    }

    /// <summary>
    /// Get tenant ID from JWT claims
    /// </summary>
    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant ID not found in token");
        }
        return tenantId;
    }

    /// <summary>
    /// Get user ID from JWT claims
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

/// <summary>
/// Request model for executing instant backup
/// </summary>
public class ExecuteInstantBackupRequest
{
    public Guid DatabaseConnectionId { get; set; }
    public string? CompressionType { get; set; } = "GZIP";
    public int? TimeoutMinutes { get; set; }
    public int? MaxRetries { get; set; }
}

/// <summary>
/// Request model for updating backup status from Worker
/// </summary>
public class UpdateBackupStatusRequest
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public BackupStatus Status { get; set; }
    
    // For InProgress status
    public int? ProgressPercentage { get; set; }
    public string? CurrentStep { get; set; }
    public long? ProcessedBytes { get; set; }
    public long? TotalBytes { get; set; }
    
    // For Completed status
    public string? BlobUrl { get; set; }
    public string? BlobName { get; set; }
    public long? BackupSizeBytes { get; set; }
    public string? CompressionType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    
    // For Failed status
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? StackTrace { get; set; }
    public int? RetryCount { get; set; }
    public bool? WillRetry { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
