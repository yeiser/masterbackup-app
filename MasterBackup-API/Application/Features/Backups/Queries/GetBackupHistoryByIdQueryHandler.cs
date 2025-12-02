using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Backups.Queries;

public class GetBackupHistoryByIdQueryHandler : IRequestHandler<GetBackupHistoryByIdQuery, BackupHistoryDto?>
{
    private readonly TenantDbContext _context;
    private readonly ILogger<GetBackupHistoryByIdQueryHandler> _logger;

    public GetBackupHistoryByIdQueryHandler(TenantDbContext context, ILogger<GetBackupHistoryByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BackupHistoryDto?> Handle(GetBackupHistoryByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching backup history by ID: {BackupHistoryId}", request.Id);

        var backupHistory = await _context.BackupHistories
            .Include(bh => bh.BackupSchedule)
            .Include(bh => bh.DatabaseConnection)
            .AsNoTracking()
            .Where(bh => bh.Id == request.Id)
            .Select(bh => new BackupHistoryDto
            {
                Id = bh.Id,
                JobId = bh.JobId,
                DatabaseConnectionId = bh.DatabaseConnectionId,
                DatabaseConnectionName = bh.DatabaseConnection != null ? bh.DatabaseConnection.Name : "Unknown",
                BackupScheduleId = bh.BackupScheduleId,
                BackupScheduleName = bh.BackupSchedule != null ? bh.BackupSchedule.Name : null,
                Status = bh.Status,
                StatusText = bh.Status.ToString(),
                StartTime = bh.StartTime,
                EndTime = bh.EndTime,
                Duration = bh.Duration,
                BlobUrl = bh.BlobUrl,
                BlobName = bh.BlobName,
                BackupSizeBytes = bh.BackupSizeBytes,
                BackupSizeMB = bh.BackupSizeMB,
                BackupSizeGB = bh.BackupSizeGB,
                ErrorMessage = bh.ErrorMessage,
                ErrorCode = bh.ErrorCode,
                RetryCount = bh.RetryCount,
                CompressionType = bh.CompressionType,
                IsInstantBackup = bh.IsInstantBackup,
                CreatedAt = bh.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (backupHistory == null)
        {
            _logger.LogWarning("Backup history {BackupHistoryId} not found", request.Id);
        }

        return backupHistory;
    }
}
