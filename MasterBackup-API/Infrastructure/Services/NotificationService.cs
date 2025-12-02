using Microsoft.AspNetCore.SignalR;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Hubs;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<BackupNotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<BackupNotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Send notification when backup starts to all tenant connections
    /// </summary>
    public async Task NotifyBackupStartedAsync(Guid tenantId, BackupStartedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync("BackupStarted", notification);

            _logger.LogInformation(
                "Sent BackupStarted notification to tenant {TenantId}, JobId: {JobId}, Schedule: {ScheduleName}",
                tenantId, notification.JobId, notification.ScheduleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupStarted notification to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Send notification when backup starts to specific schedule subscribers
    /// </summary>
    public async Task NotifyBackupStartedToScheduleAsync(Guid scheduleId, BackupStartedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"schedule_{scheduleId}")
                .SendAsync("BackupStarted", notification);

            _logger.LogInformation(
                "Sent BackupStarted notification to schedule {ScheduleId}, JobId: {JobId}",
                scheduleId, notification.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupStarted notification to schedule {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Send backup progress update to all tenant connections
    /// </summary>
    public async Task NotifyBackupProgressAsync(Guid tenantId, BackupProgressDto progress)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync("BackupProgress", progress);

            _logger.LogDebug(
                "Sent BackupProgress notification to tenant {TenantId}, JobId: {JobId}, Progress: {Progress}%",
                tenantId, progress.JobId, progress.ProgressPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupProgress notification to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Send backup progress update to specific schedule subscribers
    /// </summary>
    public async Task NotifyBackupProgressToScheduleAsync(Guid scheduleId, BackupProgressDto progress)
    {
        try
        {
            await _hubContext.Clients
                .Group($"schedule_{scheduleId}")
                .SendAsync("BackupProgress", progress);

            _logger.LogDebug(
                "Sent BackupProgress notification to schedule {ScheduleId}, JobId: {JobId}, Progress: {Progress}%",
                scheduleId, progress.JobId, progress.ProgressPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupProgress notification to schedule {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Send notification when backup completes successfully
    /// </summary>
    public async Task NotifyBackupCompletedAsync(Guid tenantId, BackupCompletedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync("BackupCompleted", notification);

            _logger.LogInformation(
                "Sent BackupCompleted notification to tenant {TenantId}, JobId: {JobId}, Success: {Success}, Size: {SizeMB:F2} MB",
                tenantId, notification.JobId, notification.Success, notification.BackupSizeMB);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupCompleted notification to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Send notification when backup completes to specific schedule subscribers
    /// </summary>
    public async Task NotifyBackupCompletedToScheduleAsync(Guid scheduleId, BackupCompletedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"schedule_{scheduleId}")
                .SendAsync("BackupCompleted", notification);

            _logger.LogInformation(
                "Sent BackupCompleted notification to schedule {ScheduleId}, JobId: {JobId}, Success: {Success}",
                scheduleId, notification.JobId, notification.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupCompleted notification to schedule {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Send notification when backup fails
    /// </summary>
    public async Task NotifyBackupFailedAsync(Guid tenantId, BackupFailedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync("BackupFailed", notification);

            _logger.LogWarning(
                "Sent BackupFailed notification to tenant {TenantId}, JobId: {JobId}, Error: {Error}, WillRetry: {WillRetry}",
                tenantId, notification.JobId, notification.ErrorMessage, notification.WillRetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupFailed notification to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Send notification when backup fails to specific schedule subscribers
    /// </summary>
    public async Task NotifyBackupFailedToScheduleAsync(Guid scheduleId, BackupFailedDto notification)
    {
        try
        {
            await _hubContext.Clients
                .Group($"schedule_{scheduleId}")
                .SendAsync("BackupFailed", notification);

            _logger.LogWarning(
                "Sent BackupFailed notification to schedule {ScheduleId}, JobId: {JobId}, Error: {Error}",
                scheduleId, notification.JobId, notification.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupFailed notification to schedule {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Send generic backup event notification
    /// </summary>
    public async Task NotifyBackupEventAsync(Guid tenantId, BackupEventDto eventDto)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync("BackupEvent", eventDto);

            _logger.LogInformation(
                "Sent BackupEvent notification to tenant {TenantId}, EventType: {EventType}, JobId: {JobId}",
                tenantId, eventDto.EventType, eventDto.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupEvent notification to tenant {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Send generic backup event to specific schedule subscribers
    /// </summary>
    public async Task NotifyBackupEventToScheduleAsync(Guid scheduleId, BackupEventDto eventDto)
    {
        try
        {
            await _hubContext.Clients
                .Group($"schedule_{scheduleId}")
                .SendAsync("BackupEvent", eventDto);

            _logger.LogInformation(
                "Sent BackupEvent notification to schedule {ScheduleId}, EventType: {EventType}, JobId: {JobId}",
                scheduleId, eventDto.EventType, eventDto.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending BackupEvent notification to schedule {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Send notification to all tenant connections
    /// </summary>
    public async Task NotifyTenantAsync(Guid tenantId, string method, object data)
    {
        try
        {
            await _hubContext.Clients
                .Group($"tenant_{tenantId}")
                .SendAsync(method, data);

            _logger.LogInformation(
                "Sent {Method} notification to tenant {TenantId}",
                method, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification to tenant {TenantId}", method, tenantId);
        }
    }

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    public async Task NotifyUserAsync(Guid userId, string method, object data)
    {
        try
        {
            await _hubContext.Clients
                .User(userId.ToString())
                .SendAsync(method, data);

            _logger.LogInformation(
                "Sent {Method} notification to user {UserId}",
                method, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification to user {UserId}", method, userId);
        }
    }

    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    public async Task NotifyAllAsync(string method, object data)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(method, data);

            _logger.LogInformation("Sent {Method} notification to all clients", method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Method} notification to all clients", method);
        }
    }

    /// <summary>
    /// Get count of connected clients for a tenant
    /// Note: This is an approximation as SignalR doesn't provide exact counts
    /// </summary>
    public async Task<int> GetConnectedClientsCountAsync(Guid tenantId)
    {
        // SignalR doesn't provide a direct way to count group members
        // This would require custom tracking in a distributed cache (Redis)
        // For now, return -1 to indicate "not implemented"
        await Task.CompletedTask;
        return -1;
    }
}
