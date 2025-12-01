using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Queries;

public class GetDatabaseConnectionByIdQueryHandler : IRequestHandler<GetDatabaseConnectionByIdQuery, DatabaseConnectionDto>
{
    private readonly TenantDbContext _context;
    private readonly MasterDbContext _masterContext;

    public GetDatabaseConnectionByIdQueryHandler(TenantDbContext context, MasterDbContext masterContext)
    {
        _context = context;
        _masterContext = masterContext;
    }

    public async Task<DatabaseConnectionDto> Handle(GetDatabaseConnectionByIdQuery request, CancellationToken cancellationToken)
    {
        var connection = await _context.DatabaseConnections
            .Include(c => c.BackupSchedules)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (connection == null)
            throw new KeyNotFoundException($"Database connection with ID {request.Id} not found");

        var user = await _masterContext.Users.FindAsync(new object[] { connection.CreatedBy }, cancellationToken);

        return new DatabaseConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            Description = connection.Description,
            Type = ((int)connection.Type).ToString(),
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            EngineVersion = connection.EngineVersion,
            SSLMode = connection.SSLMode,
            IsActive = connection.IsActive,
            CreatedBy = connection.CreatedBy,
            CreatedByName = user != null ? $"{user.FirstName} {user.LastName}" : null,
            CreatedAt = connection.CreatedAt,
            UpdatedAt = connection.UpdatedAt,
            LastTestedAt = connection.LastTestedAt,
            LastTestSuccessful = connection.LastTestSuccessful,
            LastTestStatus = connection.LastTestStatus,
            BackupSchedulesCount = connection.BackupSchedules.Count,
            AssignedWorkerId = connection.AssignedWorkerId,
            Tags = connection.Tags,
            AssignmentMode = connection.AssignmentMode.ToString()
        };
    }
}
