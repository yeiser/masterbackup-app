using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Common.Interfaces;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification when backup starts
    /// </summary>
    Task NotifyBackupStartedAsync(Guid tenantId, BackupStartedDto notification);

    /// <summary>
    /// Send notification when backup starts to specific schedule subscribers
    /// </summary>
    Task NotifyBackupStartedToScheduleAsync(Guid scheduleId, BackupStartedDto notification);

    /// <summary>
    /// Send backup progress update
    /// </summary>
    Task NotifyBackupProgressAsync(Guid tenantId, BackupProgressDto progress);

    /// <summary>
    /// Send backup progress update to specific schedule subscribers
    /// </summary>
    Task NotifyBackupProgressToScheduleAsync(Guid scheduleId, BackupProgressDto progress);

    /// <summary>
    /// Send notification when backup completes successfully
    /// </summary>
    Task NotifyBackupCompletedAsync(Guid tenantId, BackupCompletedDto notification);

    /// <summary>
    /// Send notification when backup completes to specific schedule subscribers
    /// </summary>
    Task NotifyBackupCompletedToScheduleAsync(Guid scheduleId, BackupCompletedDto notification);

    /// <summary>
    /// Send notification when backup fails
    /// </summary>
    Task NotifyBackupFailedAsync(Guid tenantId, BackupFailedDto notification);

    /// <summary>
    /// Send notification when backup fails to specific schedule subscribers
    /// </summary>
    Task NotifyBackupFailedToScheduleAsync(Guid scheduleId, BackupFailedDto notification);

    /// <summary>
    /// Send generic backup event notification
    /// </summary>
    Task NotifyBackupEventAsync(Guid tenantId, BackupEventDto eventDto);

    /// <summary>
    /// Send generic backup event to specific schedule subscribers
    /// </summary>
    Task NotifyBackupEventToScheduleAsync(Guid scheduleId, BackupEventDto eventDto);

    /// <summary>
    /// Send notification to all tenant connections
    /// </summary>
    Task NotifyTenantAsync(Guid tenantId, string method, object data);

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    Task NotifyUserAsync(Guid userId, string method, object data);

    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    Task NotifyAllAsync(string method, object data);

    /// <summary>
    /// Get count of connected clients for a tenant
    /// </summary>
    Task<int> GetConnectedClientsCountAsync(Guid tenantId);
}
