using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Handler para obtener un schedule de backup por ID
/// </summary>
public class GetBackupScheduleByIdQueryHandler : IRequestHandler<GetBackupScheduleByIdQuery, BackupScheduleDto?>
{
    private readonly TenantDbContext _context;

    public GetBackupScheduleByIdQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<BackupScheduleDto?> Handle(GetBackupScheduleByIdQuery request, CancellationToken cancellationToken)
    {
        var backupSchedule = await _context.BackupSchedules
            .Include(bs => bs.DatabaseConnection)
            .FirstOrDefaultAsync(bs => bs.Id == request.Id && bs.TenantId == request.TenantId, cancellationToken);

        if (backupSchedule == null)
        {
            return null;
        }

        return MapToDto(backupSchedule);
    }

    private static BackupScheduleDto MapToDto(Domain.Entities.BackupSchedule entity)
    {
        return new BackupScheduleDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            DatabaseConnectionId = entity.DatabaseConnectionId,
            DatabaseConnectionName = entity.DatabaseConnection?.Name ?? string.Empty,
            Name = entity.Name,
            Description = entity.Description,
            CronExpression = entity.CronExpression,
            TimeZone = entity.TimeZone,
            RetentionDays = entity.RetentionDays,
            IsActive = entity.IsActive,
            NextRun = entity.NextRun,
            LastRun = entity.LastRun,
            LastExecutionStatus = entity.LastExecutionStatus,
            LastExecutionError = entity.LastExecutionError,
            MaxRetries = entity.MaxRetries,
            TimeoutMinutes = entity.TimeoutMinutes,
            Priority = entity.Priority,
            NotifyOnCompletion = entity.NotifyOnCompletion,
            NotifyOnlyOnFailure = entity.NotifyOnlyOnFailure,
            AutoRestore = entity.AutoRestore,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }
}
