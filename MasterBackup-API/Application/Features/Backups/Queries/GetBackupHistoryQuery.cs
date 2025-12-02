using MediatR;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Backups.Queries;

/// <summary>
/// Query to get paginated backup history with filters
/// </summary>
public class GetBackupHistoryQuery : IRequest<GetBackupHistoryResult>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Filters
    public Guid? BackupScheduleId { get; set; }
    public Guid? DatabaseConnectionId { get; set; }
    public BackupStatus? Status { get; set; }
    public bool? IsInstantBackup { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    
    // Sorting
    public string SortBy { get; set; } = "StartTime";
    public string SortDirection { get; set; } = "DESC";
}

/// <summary>
/// Result with paginated backup history
/// </summary>
public class GetBackupHistoryResult
{
    public List<BackupHistoryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Backup history DTO
/// </summary>
public class BackupHistoryDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid DatabaseConnectionId { get; set; }
    public string DatabaseConnectionName { get; set; } = string.Empty;
    public Guid? BackupScheduleId { get; set; }
    public string? BackupScheduleName { get; set; }
    public BackupStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? BlobUrl { get; set; }
    public string? BlobName { get; set; }
    public long? BackupSizeBytes { get; set; }
    public double? BackupSizeMB { get; set; }
    public double? BackupSizeGB { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int RetryCount { get; set; }
    public string? CompressionType { get; set; }
    public bool IsInstantBackup { get; set; }
    public DateTime CreatedAt { get; set; }
}
