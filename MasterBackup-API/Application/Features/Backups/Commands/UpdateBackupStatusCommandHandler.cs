using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;
using System.Text.Json;

namespace MasterBackup_API.Application.Features.Backups.Commands;

public class UpdateBackupStatusCommandHandler : IRequestHandler<UpdateBackupStatusCommand, UpdateBackupStatusResult>
{
    private readonly TenantDbContext _tenantContext;
    private readonly MasterDbContext _masterContext;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<UpdateBackupStatusCommandHandler> _logger;

    public UpdateBackupStatusCommandHandler(
        TenantDbContext tenantContext,
        MasterDbContext masterContext,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<UpdateBackupStatusCommandHandler> logger)
    {
        _tenantContext = tenantContext;
        _masterContext = masterContext;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<UpdateBackupStatusResult> Handle(UpdateBackupStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating backup status for Job {JobId} to {Status}", request.JobId, request.Status);

        // Find backup history by JobId
        var backupHistory = await _tenantContext.BackupHistories
            .Include(bh => bh.BackupSchedule)
            .Include(bh => bh.DatabaseConnection)
            .FirstOrDefaultAsync(bh => bh.JobId == request.JobId, cancellationToken);

        if (backupHistory == null)
        {
            _logger.LogWarning("BackupHistory not found for Job {JobId}", request.JobId);
            return new UpdateBackupStatusResult
            {
                Success = false,
                Message = $"Backup history not found for job {request.JobId}"
            };
        }

        // Update status
        backupHistory.Status = request.Status;
        backupHistory.UpdatedAt = DateTime.UtcNow;

        // Handle status-specific updates
        switch (request.Status)
        {
            case BackupStatus.InProgress:
                await HandleInProgressStatus(backupHistory, request, cancellationToken);
                break;

            case BackupStatus.Completed:
                await HandleCompletedStatus(backupHistory, request, cancellationToken);
                break;

            case BackupStatus.Failed:
            case BackupStatus.Timeout:
                await HandleFailedStatus(backupHistory, request, cancellationToken);
                break;

            case BackupStatus.Cancelled:
                await HandleCancelledStatus(backupHistory, request, cancellationToken);
                break;
        }

        await _tenantContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated BackupHistory {BackupHistoryId} to status {Status}", 
            backupHistory.Id, request.Status);

        return new UpdateBackupStatusResult
        {
            Success = true,
            Message = "Backup status updated successfully",
            BackupHistoryId = backupHistory.Id
        };
    }

    private async Task HandleInProgressStatus(
        Domain.Entities.BackupHistory backupHistory, 
        UpdateBackupStatusCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Backup {JobId} is in progress: {ProgressPercentage}%", 
            request.JobId, request.ProgressPercentage);

        // Send SignalR progress notification
        try
        {
            var progressDto = new Application.Common.DTOs.BackupProgressDto
            {
                JobId = request.JobId,
                TenantId = request.TenantId,
                BackupScheduleId = backupHistory.BackupScheduleId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow,
                ProgressPercentage = request.ProgressPercentage ?? 0,
                CurrentStep = request.CurrentStep ?? "Processing",
                ProcessedBytes = request.ProcessedBytes ?? 0,
                TotalBytes = request.TotalBytes ?? 0,
                ElapsedTime = DateTime.UtcNow - backupHistory.StartTime,
                EstimatedTimeRemaining = CalculateEstimatedTimeRemaining(
                    backupHistory.StartTime, 
                    request.ProgressPercentage ?? 0)
            };

            await _notificationService.NotifyBackupProgressAsync(request.TenantId, progressDto);

            // Also notify schedule-specific group if this is a scheduled backup
            if (backupHistory.BackupScheduleId.HasValue && backupHistory.BackupScheduleId.Value != Guid.Empty)
            {
                await _notificationService.NotifyBackupProgressToScheduleAsync(
                    backupHistory.BackupScheduleId.Value, 
                    progressDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send progress notification for Job {JobId}", request.JobId);
        }
    }

    private async Task HandleCompletedStatus(
        Domain.Entities.BackupHistory backupHistory, 
        UpdateBackupStatusCommand request, 
        CancellationToken cancellationToken)
    {
        backupHistory.EndTime = DateTime.UtcNow;
        backupHistory.Duration = backupHistory.EndTime - backupHistory.StartTime;
        backupHistory.BlobUrl = request.BlobUrl;
        backupHistory.BlobName = request.BlobName;
        backupHistory.BackupSizeBytes = request.BackupSizeBytes;
        backupHistory.CompressionType = request.CompressionType;
        backupHistory.RetryCount = request.RetryCount ?? 0;

        if (request.Metadata != null)
        {
            backupHistory.Metadata = JsonSerializer.Serialize(request.Metadata);
        }

        _logger.LogInformation("Backup {JobId} completed successfully. Size: {SizeMB} MB", 
            request.JobId, backupHistory.BackupSizeMB);

        // Send SignalR completion notification
        try
        {
            var completedDto = new Application.Common.DTOs.BackupCompletedDto
            {
                JobId = request.JobId,
                TenantId = request.TenantId,
                BackupScheduleId = backupHistory.BackupScheduleId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow,
                Success = true,
                BlobUrl = request.BlobUrl ?? "",
                BlobName = request.BlobName ?? "",
                BackupSizeBytes = request.BackupSizeBytes ?? 0,
                Duration = backupHistory.Duration ?? TimeSpan.Zero,
                StartTime = backupHistory.StartTime,
                EndTime = backupHistory.EndTime ?? DateTime.UtcNow,
                CompressionType = request.CompressionType,
                Metadata = request.Metadata
            };

            await _notificationService.NotifyBackupCompletedAsync(request.TenantId, completedDto);

            if (backupHistory.BackupScheduleId.HasValue && backupHistory.BackupScheduleId.Value != Guid.Empty)
            {
                await _notificationService.NotifyBackupCompletedToScheduleAsync(
                    backupHistory.BackupScheduleId.Value, 
                    completedDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send completion notification for Job {JobId}", request.JobId);
        }

        // Send email notification if configured
        await SendEmailNotificationAsync(backupHistory, request, isSuccess: true, cancellationToken);
    }

    private async Task HandleFailedStatus(
        Domain.Entities.BackupHistory backupHistory, 
        UpdateBackupStatusCommand request, 
        CancellationToken cancellationToken)
    {
        backupHistory.EndTime = DateTime.UtcNow;
        backupHistory.Duration = backupHistory.EndTime - backupHistory.StartTime;
        backupHistory.ErrorMessage = request.ErrorMessage;
        backupHistory.ErrorCode = request.ErrorCode;
        backupHistory.StackTrace = request.StackTrace;
        backupHistory.RetryCount = request.RetryCount ?? 0;

        _logger.LogError("Backup {JobId} failed: {ErrorMessage}", request.JobId, request.ErrorMessage);

        // Send SignalR failure notification
        try
        {
            var failedDto = new Application.Common.DTOs.BackupFailedDto
            {
                JobId = request.JobId,
                TenantId = request.TenantId,
                BackupScheduleId = backupHistory.BackupScheduleId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow,
                ErrorMessage = request.ErrorMessage ?? "Unknown error",
                ErrorCode = request.ErrorCode,
                StackTrace = request.StackTrace,
                RetryCount = request.RetryCount ?? 0,
                MaxRetries = backupHistory.BackupSchedule?.MaxRetries ?? 3,
                WillRetry = request.WillRetry ?? false,
                NextRetryAt = request.NextRetryAt,
                Duration = backupHistory.Duration ?? TimeSpan.Zero
            };

            await _notificationService.NotifyBackupFailedAsync(request.TenantId, failedDto);

            if (backupHistory.BackupScheduleId.HasValue && backupHistory.BackupScheduleId.Value != Guid.Empty)
            {
                await _notificationService.NotifyBackupFailedToScheduleAsync(
                    backupHistory.BackupScheduleId.Value, 
                    failedDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send failure notification for Job {JobId}", request.JobId);
        }

        // Send email notification if configured
        await SendEmailNotificationAsync(backupHistory, request, isSuccess: false, cancellationToken);
    }

    private async Task HandleCancelledStatus(
        Domain.Entities.BackupHistory backupHistory, 
        UpdateBackupStatusCommand request, 
        CancellationToken cancellationToken)
    {
        backupHistory.EndTime = DateTime.UtcNow;
        backupHistory.Duration = backupHistory.EndTime - backupHistory.StartTime;
        backupHistory.ErrorMessage = "Backup was cancelled";

        _logger.LogWarning("Backup {JobId} was cancelled", request.JobId);

        // Send SignalR event notification
        try
        {
            var eventDto = new Application.Common.DTOs.BackupEventDto
            {
                EventType = "BackupCancelled",
                JobId = request.JobId,
                TenantId = request.TenantId,
                BackupScheduleId = backupHistory.BackupScheduleId ?? Guid.Empty,
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    { "message", "Backup was cancelled" },
                    { "duration", backupHistory.Duration?.ToString() ?? "00:00:00" }
                }
            };

            await _notificationService.NotifyBackupEventAsync(request.TenantId, eventDto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation notification for Job {JobId}", request.JobId);
        }
    }

    private async Task SendEmailNotificationAsync(
        Domain.Entities.BackupHistory backupHistory,
        UpdateBackupStatusCommand request,
        bool isSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if we have a backup schedule with notification settings
            if (backupHistory.BackupSchedule == null)
            {
                _logger.LogDebug("No BackupSchedule found for Job {JobId}, skipping email notification", request.JobId);
                return;
            }

            var schedule = backupHistory.BackupSchedule;

            // Apply notification rules based on schedule settings
            bool shouldNotify = false;

            if (isSuccess)
            {
                // For successful backups: only notify if NotifyOnCompletion is true AND NotifyOnlyOnFailure is false
                shouldNotify = schedule.NotifyOnCompletion && !schedule.NotifyOnlyOnFailure;
            }
            else
            {
                // For failed backups: notify if either NotifyOnCompletion OR NotifyOnlyOnFailure is true
                shouldNotify = schedule.NotifyOnCompletion || schedule.NotifyOnlyOnFailure;
            }

            if (!shouldNotify)
            {
                _logger.LogDebug("Email notification skipped for Job {JobId} based on schedule settings (NotifyOnCompletion={NotifyOnCompletion}, NotifyOnlyOnFailure={NotifyOnlyOnFailure})",
                    request.JobId, schedule.NotifyOnCompletion, schedule.NotifyOnlyOnFailure);
                return;
            }

            // Get the user who created the schedule from MasterDbContext
            var userId = schedule.CreatedBy.ToString();
            var user = await _masterContext.Users
                .Where(u => u.Id == userId && u.TenantId == request.TenantId && u.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for schedule creator {CreatedBy}, TenantId {TenantId}. Cannot send email notification.",
                    schedule.CreatedBy, request.TenantId);
                return;
            }

            // Prepare email data
            var scheduleName = schedule.Name ?? "Unnamed Schedule";
            var databaseName = backupHistory.DatabaseConnection?.Name ?? backupHistory.DatabaseConnection?.Database ?? "Unknown Database";
            var recipientName = $"{user.FirstName} {user.LastName}";

            // Send appropriate email based on success/failure
            if (isSuccess)
            {
                var fileSizeMB = backupHistory.BackupSizeMB ?? 0;
                var duration = backupHistory.Duration ?? TimeSpan.Zero;
                var completedAt = backupHistory.EndTime ?? DateTime.UtcNow;
                var blobUrl = request.BlobUrl ?? "N/A";

                await _emailService.SendBackupCompletedEmailAsync(
                    user.Email!,
                    recipientName,
                    scheduleName,
                    databaseName,
                    fileSizeMB,
                    blobUrl,
                    completedAt,
                    duration);

                _logger.LogInformation("Backup completion email sent to {Email} for Job {JobId}",
                    user.Email, request.JobId);
            }
            else
            {
                var errorMessage = request.ErrorMessage ?? "Unknown error occurred";
                var failedAt = backupHistory.EndTime ?? DateTime.UtcNow;

                await _emailService.SendBackupFailedEmailAsync(
                    user.Email!,
                    recipientName,
                    scheduleName,
                    databaseName,
                    errorMessage,
                    failedAt);

                _logger.LogInformation("Backup failure email sent to {Email} for Job {JobId}",
                    user.Email, request.JobId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the entire status update if email sending fails
            _logger.LogError(ex, "Failed to send email notification for Job {JobId}. Email notification will be skipped.",
                request.JobId);
        }
    }

    private TimeSpan? CalculateEstimatedTimeRemaining(DateTime startTime, int progressPercentage)
    {
        if (progressPercentage <= 0) return null;

        var elapsed = DateTime.UtcNow - startTime;
        var totalEstimated = elapsed.TotalSeconds * (100.0 / progressPercentage);
        var remaining = totalEstimated - elapsed.TotalSeconds;

        return TimeSpan.FromSeconds(Math.Max(0, remaining));
    }
}
