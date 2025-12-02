using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Backups.Commands;

public class DeleteBackupHistoryCommandHandler : IRequestHandler<DeleteBackupHistoryCommand, bool>
{
    private readonly TenantDbContext _context;

    public DeleteBackupHistoryCommandHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteBackupHistoryCommand request, CancellationToken cancellationToken)
    {
        var backupHistory = await _context.BackupHistories
            .FirstOrDefaultAsync(bh => bh.Id == request.Id, cancellationToken);

        if (backupHistory == null)
        {
            return false;
        }

        _context.BackupHistories.Remove(backupHistory);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
