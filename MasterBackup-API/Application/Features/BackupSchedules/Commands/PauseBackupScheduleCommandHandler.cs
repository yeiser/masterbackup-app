using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Handler para pausar un schedule de backup
/// </summary>
public class PauseBackupScheduleCommandHandler : IRequestHandler<PauseBackupScheduleCommand, bool>
{
    private readonly TenantDbContext _context;
    private readonly IBackupSchedulerService _schedulerService;

    public PauseBackupScheduleCommandHandler(
        TenantDbContext context,
        IBackupSchedulerService schedulerService)
    {
        _context = context;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(PauseBackupScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar el schedule
        var backupSchedule = await _context.BackupSchedules
            .FirstOrDefaultAsync(bs => bs.Id == request.Id && bs.TenantId == request.TenantId, cancellationToken);

        if (backupSchedule == null)
        {
            throw new ArgumentException("Backup schedule not found.");
        }

        if (!backupSchedule.IsActive)
        {
            throw new InvalidOperationException("Backup schedule is already paused.");
        }

        // 2. Marcar como inactivo
        backupSchedule.IsActive = false;
        backupSchedule.UpdatedAt = DateTime.UtcNow;
        backupSchedule.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        // 3. Pausar en Quartz.NET
        await _schedulerService.PauseBackupAsync(backupSchedule.Id, cancellationToken);

        return true;
    }
}
