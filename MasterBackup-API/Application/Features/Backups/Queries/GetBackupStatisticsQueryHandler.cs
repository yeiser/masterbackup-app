using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Backups.Queries;

public class GetBackupStatisticsQueryHandler : IRequestHandler<GetBackupStatisticsQuery, BackupStatisticsDto>
{
    private readonly TenantDbContext _context;

    public GetBackupStatisticsQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<BackupStatisticsDto> Handle(GetBackupStatisticsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BackupHistories.AsQueryable();

        // Apply filters
        if (request.BackupScheduleId.HasValue)
        {
            query = query.Where(bh => bh.BackupScheduleId == request.BackupScheduleId.Value);
        }

        if (request.DatabaseConnectionId.HasValue)
        {
            query = query.Where(bh => bh.DatabaseConnectionId == request.DatabaseConnectionId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(bh => bh.StartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(bh => bh.StartTime <= request.EndDate.Value);
        }

        var backups = await query.ToListAsync(cancellationToken);

        var totalBackups = backups.Count;
        var successfulBackups = backups.Count(b => b.Status == BackupStatus.Completed);
        var failedBackups = backups.Count(b => b.Status == BackupStatus.Failed);
        var inProgressBackups = backups.Count(b => b.Status == BackupStatus.InProgress || b.Status == BackupStatus.Pending);

        var successRate = totalBackups > 0 ? (successfulBackups / (double)totalBackups) * 100 : 0;

        var totalBackupSizeBytes = backups.Where(b => b.BackupSizeBytes.HasValue).Sum(b => b.BackupSizeBytes!.Value);
        var totalBackupSizeMB = totalBackupSizeBytes / (1024.0 * 1024.0);
        var totalBackupSizeGB = totalBackupSizeBytes / (1024.0 * 1024.0 * 1024.0);

        var completedBackups = backups.Where(b => b.Duration.HasValue).ToList();
        TimeSpan? averageDuration = null;
        TimeSpan? minDuration = null;
        TimeSpan? maxDuration = null;

        if (completedBackups.Any())
        {
            var totalTicks = completedBackups.Sum(b => b.Duration!.Value.Ticks);
            averageDuration = new TimeSpan(totalTicks / completedBackups.Count);
            minDuration = completedBackups.Min(b => b.Duration);
            maxDuration = completedBackups.Max(b => b.Duration);
        }

        var lastSuccessfulBackup = backups
            .Where(b => b.Status == BackupStatus.Completed)
            .OrderByDescending(b => b.StartTime)
            .Select(b => b.StartTime)
            .FirstOrDefault();

        var lastFailedBackup = backups
            .Where(b => b.Status == BackupStatus.Failed)
            .OrderByDescending(b => b.StartTime)
            .Select(b => b.StartTime)
            .FirstOrDefault();

        // Status breakdown
        var statusBreakdown = backups
            .GroupBy(b => b.Status)
            .Select(g => new StatusBreakdown
            {
                Status = g.Key,
                StatusText = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalBackups > 0 ? (g.Count() / (double)totalBackups) * 100 : 0
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        // Daily backup counts (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var dailyBackupCounts = backups
            .Where(b => b.StartTime >= thirtyDaysAgo)
            .GroupBy(b => b.StartTime.Date)
            .Select(g => new DailyBackupCount
            {
                Date = g.Key,
                TotalCount = g.Count(),
                SuccessCount = g.Count(b => b.Status == BackupStatus.Completed),
                FailedCount = g.Count(b => b.Status == BackupStatus.Failed)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new BackupStatisticsDto
        {
            TotalBackups = totalBackups,
            SuccessfulBackups = successfulBackups,
            FailedBackups = failedBackups,
            InProgressBackups = inProgressBackups,
            SuccessRate = successRate,
            TotalBackupSizeBytes = totalBackupSizeBytes,
            TotalBackupSizeMB = totalBackupSizeMB,
            TotalBackupSizeGB = totalBackupSizeGB,
            AverageDuration = averageDuration,
            MinDuration = minDuration,
            MaxDuration = maxDuration,
            LastSuccessfulBackup = lastSuccessfulBackup == default ? null : lastSuccessfulBackup,
            LastFailedBackup = lastFailedBackup == default ? null : lastFailedBackup,
            StatusBreakdown = statusBreakdown,
            DailyBackupCounts = dailyBackupCounts
        };
    }
}
