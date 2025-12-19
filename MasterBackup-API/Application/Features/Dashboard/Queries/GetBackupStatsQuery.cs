using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Dashboard.Queries;

public class GetBackupStatsQuery : IRequest<BackupStatsDto>
{
}
