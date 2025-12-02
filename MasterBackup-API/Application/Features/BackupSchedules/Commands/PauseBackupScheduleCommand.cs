using MediatR;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Command para pausar un schedule de backup (IsActive = false)
/// </summary>
public class PauseBackupScheduleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UpdatedBy { get; set; }
}
