using MediatR;

namespace MasterBackup_API.Application.Features.Backups.Commands;

/// <summary>
/// Command to delete a backup history record
/// </summary>
public class DeleteBackupHistoryCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
