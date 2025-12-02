namespace MasterBackup_API.Domain.Models;

/// <summary>
/// Result message sent from Worker to API after backup execution
/// </summary>
public class BackupJobResult
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BackupScheduleId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long BackupSizeBytes { get; set; }
    public string? BlobUrl { get; set; } // URL of uploaded backup file
    public string? BlobName { get; set; } // Name of blob in storage
    public int RetryCount { get; set; }
    public Dictionary<string, string>? Metadata { get; set; } // Additional info (database version, etc.)
}
