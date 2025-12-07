using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Handler para crear un nuevo schedule de backup
/// </summary>
public class CreateBackupScheduleCommandHandler : IRequestHandler<CreateBackupScheduleCommand, BackupScheduleDto>
{
    private readonly TenantDbContext _context;
    private readonly IBackupSchedulerService _schedulerService;

    public CreateBackupScheduleCommandHandler(
        TenantDbContext context,
        IBackupSchedulerService schedulerService)
    {
        _context = context;
        _schedulerService = schedulerService;
    }

    public async Task<BackupScheduleDto> Handle(CreateBackupScheduleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validar que la DatabaseConnection existe y está activa
            // Nota: DatabaseConnection no tiene TenantId, la validación de tenant se hace via TenantDbContext
            var databaseConnection = await _context.DatabaseConnections
                .FirstOrDefaultAsync(dc => dc.Id == request.Data.DatabaseConnectionId
                    && dc.IsActive, cancellationToken);

            if (databaseConnection == null)
            {
                throw new ArgumentException("Database connection not found or inactive.");
            }

            // 2. Validar expresión CRON si se proporciona (solo para tipo Cron)
            if (!string.IsNullOrWhiteSpace(request.Data.CronExpression) &&
                !_schedulerService.IsValidCronExpression(request.Data.CronExpression))
            {
                throw new ArgumentException($"Invalid CRON expression format: '{request.Data.CronExpression}'. " +
                    "Expected Quartz.NET format with 6 fields: second minute hour day month dayOfWeek " +
                    "(example: '0 0 2 * * ?' for daily at 2 AM)");
            }

            // 3. Crear entidad BackupSchedule
            var backupSchedule = new BackupSchedule
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                DatabaseConnectionId = request.Data.DatabaseConnectionId,
                Name = request.Data.Name,
                Description = request.Data.Description,
                CronExpression = request.Data.CronExpression,
                TimeZone = request.Data.TimeZone,
                RetentionDays = request.Data.RetentionDays,
                IsActive = request.Data.IsActive,
                MaxRetries = request.Data.MaxRetries,
                TimeoutMinutes = request.Data.TimeoutMinutes,
                Priority = request.Data.Priority,
                NotifyOnCompletion = request.Data.NotifyOnCompletion,
                NotifyOnlyOnFailure = request.Data.NotifyOnlyOnFailure,
                AutoRestore = request.Data.AutoRestore,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
                // NextRun se calculará con Quartz.NET
            };

            _context.BackupSchedules.Add(backupSchedule);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Registrar job en Quartz.NET si está activo
            if (backupSchedule.IsActive)
            {
                await _schedulerService.ScheduleBackupAsync(backupSchedule, cancellationToken);

                // 5. Calcular y actualizar NextRun
                var nextRun = await _schedulerService.GetNextFireTimeAsync(backupSchedule.Id, cancellationToken);
                if (nextRun.HasValue)
                {
                    // Asegurar que el DateTime tenga Kind = UTC para PostgreSQL
                    backupSchedule.NextRun = DateTime.SpecifyKind(nextRun.Value, DateTimeKind.Utc);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            // 6. Cargar relaciones para el DTO
            await _context.Entry(backupSchedule)
                .Reference(bs => bs.DatabaseConnection)
                .LoadAsync(cancellationToken);

            // 7. Mapear a DTO
            return MapToDto(backupSchedule);
        }
        catch(Exception ex)
        {
            throw new Exception(ex.Message, ex);
        }
    }

    private static BackupScheduleDto MapToDto(BackupSchedule entity)
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
