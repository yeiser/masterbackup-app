using MediatR;

namespace MasterBackup_API.Application.Features.Backups.Queries;

/// <summary>
/// Query to get a single backup history record by ID
/// </summary>
public class GetBackupHistoryByIdQuery : IRequest<BackupHistoryDto?>
{
    public Guid Id { get; set; }
}
