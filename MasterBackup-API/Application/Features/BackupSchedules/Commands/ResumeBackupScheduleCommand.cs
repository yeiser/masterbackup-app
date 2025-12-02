using MediatR;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Command para reanudar un schedule de backup pausado (IsActive = true)
/// </summary>
public class ResumeBackupScheduleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UpdatedBy { get; set; }
}
