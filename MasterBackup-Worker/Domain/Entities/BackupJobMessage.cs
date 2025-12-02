namespace MasterBackup_Worker.Domain.Entities;

/// <summary>
/// Message sent from API to Worker to execute a backup job
/// </summary>
public class BackupJobMessage
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BackupScheduleId { get; set; }
    public DatabaseConnectionInfo DatabaseConnection { get; set; } = null!;
    public string BlobStorageConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty; // backups-{tenantId}
    public string BackupFileName { get; set; } = string.Empty; // {scheduleName}_{timestamp}.backup
    public int TimeoutMinutes { get; set; }
    public int MaxRetries { get; set; }
    public int CurrentRetry { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string? CompressionType { get; set; } // gzip, none, etc.
    public string? EncryptionKey { get; set; } // For future encrypted backups
}

/// <summary>
/// Database connection information for backup execution
/// </summary>
public class DatabaseConnectionInfo
{
    public Guid DatabaseConnectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty; // PostgreSQL, MySQL, SQLServer, MongoDB
}
