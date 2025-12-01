using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Queries;

public class GetDatabaseConnectionsQueryHandler : IRequestHandler<GetDatabaseConnectionsQuery, List<DatabaseConnectionDto>>
{
    private readonly TenantDbContext _context;
    private readonly MasterDbContext _masterContext;

    public GetDatabaseConnectionsQueryHandler(TenantDbContext context, MasterDbContext masterContext)
    {
        _context = context;
        _masterContext = masterContext;
    }

    public async Task<List<DatabaseConnectionDto>> Handle(GetDatabaseConnectionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var connections = await _context.DatabaseConnections
            .Include(c => c.BackupSchedules)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

            // Get unique user IDs as string (IdentityUser.Id is string)
            var userIds = connections.Select(c => c.CreatedBy.ToString()).Distinct().ToList();
            
            // Query users with string comparison
            var users = await _masterContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

            return connections.Select(c => new DatabaseConnectionDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = ((int)c.Type).ToString(),
                Host = c.Host,
                Port = c.Port,
                Database = c.Database,
                Username = c.Username,
                EngineVersion = c.EngineVersion,
                SSLMode = c.SSLMode,
                IsActive = c.IsActive,
                CreatedBy = c.CreatedBy,
                CreatedByName = users.GetValueOrDefault(c.CreatedBy.ToString()),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                LastTestedAt = c.LastTestedAt,
                LastTestSuccessful = c.LastTestSuccessful,
                LastTestStatus = c.LastTestStatus,
                BackupSchedulesCount = c.BackupSchedules.Count,
                AssignedWorkerId = c.AssignedWorkerId,
                Tags = c.Tags,
                AssignmentMode = c.AssignmentMode.ToString()
            }).ToList();
        }
        catch(Exception ex)
        {
            throw;
        }
    }
}
