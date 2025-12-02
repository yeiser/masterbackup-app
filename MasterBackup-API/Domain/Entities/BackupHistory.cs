using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

/// <summary>
/// Backup execution history record stored in tenant database
/// </summary>
public class BackupHistory
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Foreign key to BackupSchedule (null for instant backups)
    /// </summary>
    public Guid? BackupScheduleId { get; set; }
    public BackupSchedule? BackupSchedule { get; set; }
    
    /// <summary>
    /// Job ID from RabbitMQ message for tracking
    /// </summary>
    public Guid JobId { get; set; }
    
    /// <summary>
    /// Foreign key to DatabaseConnection
    /// </summary>
    public Guid DatabaseConnectionId { get; set; }
    public DatabaseConnection? DatabaseConnection { get; set; }
    
    /// <summary>
    /// Backup execution status
    /// </summary>
    public BackupStatus Status { get; set; }
    
    /// <summary>
    /// When backup execution started
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When backup execution ended (null if in progress)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Duration of backup execution
    /// </summary>
    public TimeSpan? Duration { get; set; }
    
    /// <summary>
    /// Azure Blob Storage URL where backup is stored
    /// </summary>
    public string? BlobUrl { get; set; }
    
    /// <summary>
    /// Name of the blob file
    /// </summary>
    public string? BlobName { get; set; }
    
    /// <summary>
    /// Size of backup file in bytes
    /// </summary>
    public long? BackupSizeBytes { get; set; }
    
    /// <summary>
    /// Size in MB for convenience
    /// </summary>
    public double? BackupSizeMB => BackupSizeBytes.HasValue ? BackupSizeBytes.Value / (1024.0 * 1024.0) : null;
    
    /// <summary>
    /// Size in GB for convenience
    /// </summary>
    public double? BackupSizeGB => BackupSizeBytes.HasValue ? BackupSizeBytes.Value / (1024.0 * 1024.0 * 1024.0) : null;
    
    /// <summary>
    /// Error message if backup failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Error code for categorizing failures
    /// </summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// Stack trace if available
    /// </summary>
    public string? StackTrace { get; set; }
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Compression type used (GZIP, BZIP2, etc.)
    /// </summary>
    public string? CompressionType { get; set; }
    
    /// <summary>
    /// Additional metadata stored as JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Whether this was an instant (manual) backup
    /// </summary>
    public bool IsInstantBackup { get; set; }
    
    /// <summary>
    /// Timestamp of record creation
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
