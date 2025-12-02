namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// Base notification for backup events
/// </summary>
public class BackupNotificationDto
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BackupScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty; // Started, Progress, Completed, Failed
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Notification when backup starts
/// </summary>
public class BackupStartedDto : BackupNotificationDto
{
    public BackupStartedDto()
    {
        NotificationType = "Started";
    }

    public string DatabaseType { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
}

/// <summary>
/// Notification for backup progress updates
/// </summary>
public class BackupProgressDto : BackupNotificationDto
{
    public BackupProgressDto()
    {
        NotificationType = "Progress";
    }

    public int ProgressPercentage { get; set; }
    public string CurrentStep { get; set; } = string.Empty; // Dumping, Compressing, Uploading
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double ProcessedMB => ProcessedBytes / 1024.0 / 1024.0;
    public double TotalMB => TotalBytes / 1024.0 / 1024.0;
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Notification when backup completes successfully
/// </summary>
public class BackupCompletedDto : BackupNotificationDto
{
    public BackupCompletedDto()
    {
        NotificationType = "Completed";
    }

    public bool Success { get; set; }
    public string? BlobUrl { get; set; }
    public string? BlobName { get; set; }
    public long BackupSizeBytes { get; set; }
    public double BackupSizeMB => BackupSizeBytes / 1024.0 / 1024.0;
    public double BackupSizeGB => BackupSizeBytes / 1024.0 / 1024.0 / 1024.0;
    public TimeSpan Duration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? CompressionType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Notification when backup fails
/// </summary>
public class BackupFailedDto : BackupNotificationDto
{
    public BackupFailedDto()
    {
        NotificationType = "Failed";
    }

    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? StackTrace { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public bool WillRetry { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Generic notification for any backup event
/// </summary>
public class BackupEventDto
{
    public string EventType { get; set; } = string.Empty; // Started, Progress, Completed, Failed, Queued, Cancelled
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BackupScheduleId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Data { get; set; } = new();
}
