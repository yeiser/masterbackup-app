using MediatR;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Backups.Queries;

/// <summary>
/// Query to get backup statistics
/// </summary>
public class GetBackupStatisticsQuery : IRequest<BackupStatisticsDto>
{
    public Guid? BackupScheduleId { get; set; }
    public Guid? DatabaseConnectionId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Backup statistics DTO
/// </summary>
public class BackupStatisticsDto
{
    public int TotalBackups { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public int InProgressBackups { get; set; }
    public double SuccessRate { get; set; }
    public long TotalBackupSizeBytes { get; set; }
    public double TotalBackupSizeMB { get; set; }
    public double TotalBackupSizeGB { get; set; }
    public TimeSpan? AverageDuration { get; set; }
    public TimeSpan? MinDuration { get; set; }
    public TimeSpan? MaxDuration { get; set; }
    public DateTime? LastSuccessfulBackup { get; set; }
    public DateTime? LastFailedBackup { get; set; }
    public List<StatusBreakdown> StatusBreakdown { get; set; } = new();
    public List<DailyBackupCount> DailyBackupCounts { get; set; } = new();
}

public class StatusBreakdown
{
    public BackupStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class DailyBackupCount
{
    public DateTime Date { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
