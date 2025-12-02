using MediatR;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Backups.Commands;

/// <summary>
/// Command to update backup status from Worker
/// </summary>
public class UpdateBackupStatusCommand : IRequest<UpdateBackupStatusResult>
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

/// <summary>
/// Result of update backup status operation
/// </summary>
public class UpdateBackupStatusResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? BackupHistoryId { get; set; }
}
