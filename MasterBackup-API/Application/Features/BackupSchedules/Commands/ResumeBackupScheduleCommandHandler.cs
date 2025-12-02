using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Handler para reanudar un schedule de backup pausado
/// </summary>
public class ResumeBackupScheduleCommandHandler : IRequestHandler<ResumeBackupScheduleCommand, bool>
{
    private readonly TenantDbContext _context;
    private readonly IBackupSchedulerService _schedulerService;

    public ResumeBackupScheduleCommandHandler(
        TenantDbContext context,
        IBackupSchedulerService schedulerService)
    {
        _context = context;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(ResumeBackupScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar el schedule
        var backupSchedule = await _context.BackupSchedules
            .FirstOrDefaultAsync(bs => bs.Id == request.Id && bs.TenantId == request.TenantId, cancellationToken);

        if (backupSchedule == null)
        {
            throw new ArgumentException("Backup schedule not found.");
        }

        if (backupSchedule.IsActive)
        {
            throw new InvalidOperationException("Backup schedule is already active.");
        }

        // 2. Marcar como activo
        backupSchedule.IsActive = true;
        backupSchedule.UpdatedAt = DateTime.UtcNow;
        backupSchedule.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Reanudar en Quartz.NET
        await _schedulerService.ResumeBackupAsync(backupSchedule.Id, cancellationToken);
        
        // 4. Actualizar NextRun
        var nextRun = await _schedulerService.GetNextFireTimeAsync(backupSchedule.Id, cancellationToken);
        if (nextRun.HasValue)
        {
            backupSchedule.NextRun = nextRun.Value;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
