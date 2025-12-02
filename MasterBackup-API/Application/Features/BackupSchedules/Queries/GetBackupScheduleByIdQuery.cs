using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Query para obtener un schedule de backup por ID
/// </summary>
public class GetBackupScheduleByIdQuery : IRequest<BackupScheduleDto?>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
