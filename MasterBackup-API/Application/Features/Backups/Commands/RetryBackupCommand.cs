using MediatR;

namespace MasterBackup_API.Application.Features.Backups.Commands;

/// <summary>
/// Command to retry a failed backup
/// </summary>
public class RetryBackupCommand : IRequest<Guid>
{
    public Guid BackupHistoryId { get; set; }
}
