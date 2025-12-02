using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Backups.Commands;

public class RetryBackupCommandHandler : IRequestHandler<RetryBackupCommand, Guid>
{
    private readonly TenantDbContext _context;
    private readonly IMediator _mediator;

    public RetryBackupCommandHandler(TenantDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(RetryBackupCommand request, CancellationToken cancellationToken)
    {
        var originalBackup = await _context.BackupHistories
            .Include(bh => bh.DatabaseConnection)
            .FirstOrDefaultAsync(bh => bh.Id == request.BackupHistoryId, cancellationToken);

        if (originalBackup == null)
        {
            throw new InvalidOperationException($"Backup history with ID {request.BackupHistoryId} not found");
        }

        if (originalBackup.DatabaseConnection == null)
        {
            throw new InvalidOperationException("Database connection not found for this backup");
        }

        // Create instant backup command for retry
        var instantBackupCommand = new ExecuteInstantBackupCommand
        {
            DatabaseConnectionId = originalBackup.DatabaseConnectionId,
            CompressionType = originalBackup.CompressionType
        };

        // Execute the instant backup
        var result = await _mediator.Send(instantBackupCommand, cancellationToken);

        return result.JobId;
    }
}
