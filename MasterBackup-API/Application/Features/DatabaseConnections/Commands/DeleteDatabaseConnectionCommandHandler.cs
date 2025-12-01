using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public class DeleteDatabaseConnectionCommandHandler : IRequestHandler<DeleteDatabaseConnectionCommand, bool>
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DeleteDatabaseConnectionCommandHandler> _logger;

    public DeleteDatabaseConnectionCommandHandler(
        TenantDbContext context,
        ILogger<DeleteDatabaseConnectionCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteDatabaseConnectionCommand request, CancellationToken cancellationToken)
    {
        var connection = await _context.DatabaseConnections
            .Include(c => c.BackupSchedules)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (connection == null)
            throw new KeyNotFoundException($"Database connection with ID {request.Id} not found");

        // Verificar que no tenga schedules activos
        if (connection.BackupSchedules.Any(s => s.IsActive))
        {
            throw new InvalidOperationException("Cannot delete database connection with active backup schedules. Please deactivate or delete all schedules first.");
        }

        _context.DatabaseConnections.Remove(connection);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Database connection {Name} deleted successfully", connection.Name);

        return true;
    }
}
