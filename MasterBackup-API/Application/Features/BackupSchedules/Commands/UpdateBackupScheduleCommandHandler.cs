using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Handler para actualizar un schedule de backup existente
/// </summary>
public class UpdateBackupScheduleCommandHandler : IRequestHandler<UpdateBackupScheduleCommand, BackupScheduleDto>
{
    private readonly TenantDbContext _context;
    private readonly IBackupSchedulerService _schedulerService;

    public UpdateBackupScheduleCommandHandler(
        TenantDbContext context,
        IBackupSchedulerService schedulerService)
    {
        _context = context;
        _schedulerService = schedulerService;
    }

    public async Task<BackupScheduleDto> Handle(UpdateBackupScheduleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Buscar el schedule existente
            var backupSchedule = await _context.BackupSchedules
                .Include(bs => bs.DatabaseConnection)
                .FirstOrDefaultAsync(bs => bs.Id == request.Id && bs.TenantId == request.TenantId, cancellationToken);

            if (backupSchedule == null)
            {
                throw new ArgumentException("Backup schedule not found.");
            }

            // 2. Validar expresión CRON si se proporciona (solo para tipo Cron)
            if (!string.IsNullOrWhiteSpace(request.Data.CronExpression) &&
                !_schedulerService.IsValidCronExpression(request.Data.CronExpression))
            {
                throw new ArgumentException($"Invalid CRON expression format: '{request.Data.CronExpression}'. " +
                    "Expected Quartz.NET format with 6 fields: second minute hour day month dayOfWeek " +
                    "(example: '0 0 2 * * ?' for daily at 2 AM)");
            }

            // 3. Actualizar propiedades
            var cronChanged = backupSchedule.CronExpression != request.Data.CronExpression
                || backupSchedule.TimeZone != request.Data.TimeZone;

            backupSchedule.Name = request.Data.Name;
            backupSchedule.Description = request.Data.Description;
            backupSchedule.CronExpression = request.Data.CronExpression;
            backupSchedule.TimeZone = request.Data.TimeZone;
            backupSchedule.RetentionDays = request.Data.RetentionDays;
            backupSchedule.MaxRetries = request.Data.MaxRetries;
            backupSchedule.TimeoutMinutes = request.Data.TimeoutMinutes;
            backupSchedule.Priority = request.Data.Priority;
            backupSchedule.NotifyOnCompletion = request.Data.NotifyOnCompletion;
            backupSchedule.NotifyOnlyOnFailure = request.Data.NotifyOnlyOnFailure;
            backupSchedule.UpdatedAt = DateTime.UtcNow;
            backupSchedule.UpdatedBy = request.UpdatedBy;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Si cambió el CRON o TimeZone, reprogramar en Quartz.NET
            if (cronChanged && backupSchedule.IsActive)
            {
                await _schedulerService.RescheduleBackupAsync(backupSchedule, cancellationToken);

                // Actualizar NextRun
                var nextRun = await _schedulerService.GetNextFireTimeAsync(backupSchedule.Id, cancellationToken);
                if (nextRun.HasValue)
                {
                    // Asegurar que el DateTime tenga Kind = UTC para PostgreSQL
                    backupSchedule.NextRun = DateTime.SpecifyKind(nextRun.Value, DateTimeKind.Utc);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            // 5. Mapear a DTO
            return MapToDto(backupSchedule);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
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
