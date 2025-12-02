using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Backups.Queries;

public class GetBackupHistoryQueryHandler : IRequestHandler<GetBackupHistoryQuery, GetBackupHistoryResult>
{
    private readonly TenantDbContext _context;
    private readonly ILogger<GetBackupHistoryQueryHandler> _logger;

    public GetBackupHistoryQueryHandler(TenantDbContext context, ILogger<GetBackupHistoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GetBackupHistoryResult> Handle(GetBackupHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching backup history with filters: BackupScheduleId={BackupScheduleId}, Status={Status}, IsInstant={IsInstant}",
            request.BackupScheduleId, request.Status, request.IsInstantBackup);

        // Build query with filters
        var query = _context.BackupHistories
            .Include(bh => bh.BackupSchedule)
            .Include(bh => bh.DatabaseConnection)
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        if (request.BackupScheduleId.HasValue)
        {
            query = query.Where(bh => bh.BackupScheduleId == request.BackupScheduleId.Value);
        }

        if (request.DatabaseConnectionId.HasValue)
        {
            query = query.Where(bh => bh.DatabaseConnectionId == request.DatabaseConnectionId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(bh => bh.Status == request.Status.Value);
        }

        if (request.IsInstantBackup.HasValue)
        {
            query = query.Where(bh => bh.IsInstantBackup == request.IsInstantBackup.Value);
        }

        if (request.StartDateFrom.HasValue)
        {
            query = query.Where(bh => bh.StartTime >= request.StartDateFrom.Value);
        }

        if (request.StartDateTo.HasValue)
        {
            query = query.Where(bh => bh.StartTime <= request.StartDateTo.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "starttime" => request.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(bh => bh.StartTime) 
                : query.OrderByDescending(bh => bh.StartTime),
            "endtime" => request.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(bh => bh.EndTime) 
                : query.OrderByDescending(bh => bh.EndTime),
            "status" => request.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(bh => bh.Status) 
                : query.OrderByDescending(bh => bh.Status),
            "backupsizebytes" => request.SortDirection.ToUpper() == "ASC" 
                ? query.OrderBy(bh => bh.BackupSizeBytes) 
                : query.OrderByDescending(bh => bh.BackupSizeBytes),
            _ => query.OrderByDescending(bh => bh.StartTime)
        };

        // Apply pagination
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new GetBackupHistoryResult
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages
        };
    }
}
