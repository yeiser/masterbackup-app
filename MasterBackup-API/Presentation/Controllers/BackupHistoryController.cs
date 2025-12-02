using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MasterBackup_API.Application.Features.Backups.Commands;
using MasterBackup_API.Application.Features.Backups.Queries;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Middleware;

namespace MasterBackup_API.Presentation.Controllers;

/// <summary>
/// Controller for backup history management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupHistoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BackupHistoryController> _logger;

    public BackupHistoryController(IMediator mediator, ILogger<BackupHistoryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated backup history with filters
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="backupScheduleId">Filter by backup schedule ID</param>
    /// <param name="databaseConnectionId">Filter by database connection ID</param>
    /// <param name="status">Filter by backup status</param>
    /// <param name="isInstantBackup">Filter by instant backup flag</param>
    /// <param name="startDateFrom">Filter by start date from</param>
    /// <param name="startDateTo">Filter by start date to</param>
    /// <param name="sortBy">Sort field (default: StartTime)</param>
    /// <param name="sortDirection">Sort direction: ASC or DESC (default: DESC)</param>
    /// <returns>Paginated backup history</returns>
    [HttpGet]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<GetBackupHistoryResult>> GetBackupHistory(
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
            // Validate pagination
            if (pageNumber < 1)
            {
                return BadRequest(new { message = "Page number must be greater than 0" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Page size must be between 1 and 100" });
            }

            var query = new GetBackupHistoryQuery
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
                SortDirection = sortDirection.ToUpper()
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup history");
            return StatusCode(500, new { message = "Error retrieving backup history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get backup history by ID
    /// </summary>
    /// <param name="id">Backup history ID</param>
    /// <returns>Backup history details</returns>
    [HttpGet("{id}")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<BackupHistoryDto>> GetBackupHistoryById(Guid id)
    {
        try
        {
            var query = new GetBackupHistoryByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(new { message = $"Backup history with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup history by ID: {Id}", id);
            return StatusCode(500, new { message = "Error retrieving backup history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get backup statistics
    /// </summary>
    /// <param name="backupScheduleId">Filter by backup schedule ID</param>
    /// <param name="databaseConnectionId">Filter by database connection ID</param>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Backup statistics</returns>
    [HttpGet("statistics")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<BackupStatisticsDto>> GetBackupStatistics(
        [FromQuery] Guid? backupScheduleId = null,
        [FromQuery] Guid? databaseConnectionId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var query = new GetBackupStatisticsQuery
            {
                BackupScheduleId = backupScheduleId,
                DatabaseConnectionId = databaseConnectionId,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup statistics");
            return StatusCode(500, new { message = "Error retrieving backup statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Retry a failed backup
    /// </summary>
    /// <param name="id">Backup history ID to retry</param>
    /// <returns>New job ID for the retry</returns>
    [HttpPost("{id}/retry")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult<Guid>> RetryBackup(Guid id)
    {
        try
        {
            var command = new RetryBackupCommand { BackupHistoryId = id };
            var newJobId = await _mediator.Send(command);

            return Ok(new
            {
                message = "Backup retry initiated successfully",
                originalBackupId = id,
                newJobId = newJobId
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when retrying backup: {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying backup: {Id}", id);
            return StatusCode(500, new { message = "Error retrying backup", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a backup history record
    /// </summary>
    /// <param name="id">Backup history ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<ActionResult> DeleteBackupHistory(Guid id)
    {
        try
        {
            var command = new DeleteBackupHistoryCommand { Id = id };
            var result = await _mediator.Send(command);

            if (!result)
            {
                return NotFound(new { message = $"Backup history with ID {id} not found" });
            }

            return Ok(new { message = "Backup history deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup history: {Id}", id);
            return StatusCode(500, new { message = "Error deleting backup history", error = ex.Message });
        }
    }

    /// <summary>
    /// Bulk delete backup history records
    /// </summary>
    /// <param name="ids">Array of backup history IDs to delete</param>
    /// <returns>Number of records deleted</returns>
    [HttpPost("bulk-delete")]
    [RoleAuthorization(UserRole.Admin)]
    public async Task<ActionResult> BulkDeleteBackupHistory([FromBody] Guid[] ids)
    {
        try
        {
            if (ids == null || ids.Length == 0)
            {
                return BadRequest(new { message = "No IDs provided for deletion" });
            }

            int deletedCount = 0;
            foreach (var id in ids)
            {
                var command = new DeleteBackupHistoryCommand { Id = id };
                var result = await _mediator.Send(command);
                if (result)
                {
                    deletedCount++;
                }
            }

            return Ok(new
            {
                message = $"Successfully deleted {deletedCount} of {ids.Length} backup history records",
                deletedCount = deletedCount,
                totalRequested = ids.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deleting backup history");
            return StatusCode(500, new { message = "Error bulk deleting backup history", error = ex.Message });
        }
    }

    /// <summary>
    /// Download backup file from blob storage
    /// </summary>
    /// <param name="id">Backup history ID</param>
    /// <returns>Backup file or redirect URL</returns>
    [HttpGet("{id}/download")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult> DownloadBackup(Guid id)
    {
        try
        {
            var query = new GetBackupHistoryByIdQuery { Id = id };
            var backupHistory = await _mediator.Send(query);

            if (backupHistory == null)
            {
                return NotFound(new { message = $"Backup history with ID {id} not found" });
            }

            if (string.IsNullOrEmpty(backupHistory.BlobUrl))
            {
                return NotFound(new { message = "Backup file URL not available" });
            }

            // Return the blob URL for client-side download
            // In a production scenario, you might want to generate a SAS token for secure access
            return Ok(new
            {
                blobUrl = backupHistory.BlobUrl,
                blobName = backupHistory.BlobName,
                backupSizeBytes = backupHistory.BackupSizeBytes,
                backupSizeMB = backupHistory.BackupSizeMB,
                message = "Use the provided URL to download the backup file"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download URL for backup: {Id}", id);
            return StatusCode(500, new { message = "Error getting download URL", error = ex.Message });
        }
    }

    /// <summary>
    /// Get backup history grouped by database connection
    /// </summary>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <returns>Backup history grouped by database</returns>
    [HttpGet("by-database")]
    [RoleAuthorization(UserRole.Admin, UserRole.User)]
    public async Task<ActionResult> GetBackupHistoryByDatabase(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Get all backups within date range
            var query = new GetBackupHistoryQuery
            {
                PageNumber = 1,
                PageSize = 1000, // Get all for grouping
                StartDateFrom = startDate,
                StartDateTo = endDate,
                SortBy = "StartTime",
                SortDirection = "DESC"
            };

            var result = await _mediator.Send(query);

            // Group by database connection
            var groupedResult = result.Items
                .GroupBy(b => new { b.DatabaseConnectionId, b.DatabaseConnectionName })
                .Select(g => new
                {
                    databaseConnectionId = g.Key.DatabaseConnectionId,
                    databaseConnectionName = g.Key.DatabaseConnectionName,
                    totalBackups = g.Count(),
                    successfulBackups = g.Count(b => b.Status == BackupStatus.Completed),
                    failedBackups = g.Count(b => b.Status == BackupStatus.Failed),
                    lastBackup = g.OrderByDescending(b => b.StartTime).FirstOrDefault(),
                    totalSizeGB = g.Where(b => b.BackupSizeGB.HasValue).Sum(b => b.BackupSizeGB!.Value),
                    backups = g.OrderByDescending(b => b.StartTime).Take(10).ToList()
                })
                .OrderByDescending(g => g.lastBackup?.StartTime)
                .ToList();

            return Ok(groupedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup history by database");
            return StatusCode(500, new { message = "Error retrieving backup history by database", error = ex.Message });
        }
    }
}
