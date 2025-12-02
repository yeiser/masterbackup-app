using MediatR;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Command para eliminar un schedule de backup
/// </summary>
public class DeleteBackupScheduleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
