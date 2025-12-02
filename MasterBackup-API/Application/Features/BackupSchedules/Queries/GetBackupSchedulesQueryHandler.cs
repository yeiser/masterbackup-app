using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Handler para obtener lista de schedules de backup
/// </summary>
public class GetBackupSchedulesQueryHandler : IRequestHandler<GetBackupSchedulesQuery, List<BackupScheduleDto>>
{
    private readonly TenantDbContext _context;

    public GetBackupSchedulesQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<BackupScheduleDto>> Handle(GetBackupSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BackupSchedules
            .Include(bs => bs.DatabaseConnection)
            .Where(bs => bs.TenantId == request.TenantId);

        // Aplicar filtros
        if (request.DatabaseConnectionId.HasValue)
        {
            query = query.Where(bs => bs.DatabaseConnectionId == request.DatabaseConnectionId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(bs => bs.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(bs => bs.Name.ToLower().Contains(searchTerm) 
                || (bs.Description != null && bs.Description.ToLower().Contains(searchTerm)));
        }

        // Ordenar por próxima ejecución (los más cercanos primero)
        query = query.OrderBy(bs => bs.NextRun ?? DateTime.MaxValue);

        // Aplicar paginación
        var skip = (request.Page - 1) * request.PageSize;
        var backupSchedules = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Mapear a DTOs
        return backupSchedules.Select(MapToDto).ToList();
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
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }
}
