using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.BackupSchedules.Commands;

/// <summary>
/// Command para crear un nuevo schedule de backup
/// </summary>
public class CreateBackupScheduleCommand : IRequest<BackupScheduleDto>
{
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public CreateBackupScheduleDto Data { get; set; } = null!;
}
