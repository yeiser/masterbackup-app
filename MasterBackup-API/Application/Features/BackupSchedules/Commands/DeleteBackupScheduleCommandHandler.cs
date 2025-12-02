using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Handler para eliminar un schedule de backup
/// </summary>
public class DeleteBackupScheduleCommandHandler : IRequestHandler<DeleteBackupScheduleCommand, bool>
{
    private readonly TenantDbContext _context;
    private readonly IBackupSchedulerService _schedulerService;

    public DeleteBackupScheduleCommandHandler(
        TenantDbContext context,
        IBackupSchedulerService schedulerService)
    {
        _context = context;
        _schedulerService = schedulerService;
    }

    public async Task<bool> Handle(DeleteBackupScheduleCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar el schedule
        var backupSchedule = await _context.BackupSchedules
            .FirstOrDefaultAsync(bs => bs.Id == request.Id && bs.TenantId == request.TenantId, cancellationToken);

        if (backupSchedule == null)
        {
            throw new ArgumentException("Backup schedule not found.");
        }

        // 2. Desprogramar de Quartz.NET
        if (backupSchedule.IsActive)
        {
            await _schedulerService.UnscheduleBackupAsync(backupSchedule.Id, cancellationToken);
        }

        // 3. Eliminar de la base de datos
        _context.BackupSchedules.Remove(backupSchedule);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
