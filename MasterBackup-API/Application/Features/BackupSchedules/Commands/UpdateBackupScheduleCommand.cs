using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Command para actualizar un schedule de backup existente
/// </summary>
public class UpdateBackupScheduleCommand : IRequest<BackupScheduleDto>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UpdatedBy { get; set; }
    public UpdateBackupScheduleDto Data { get; set; } = null!;
}
