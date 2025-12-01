using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Queries;

public class GetWorkersQueryHandler : IRequestHandler<GetWorkersQuery, List<WorkerDto>>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;

    public GetWorkersQueryHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
    }

    public async Task<List<WorkerDto>> Handle(GetWorkersQuery request, CancellationToken cancellationToken)
    {
        // 1. Validar que el tenant esté autenticado
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el tenant");
        }

        // 2. Obtener workers del tenant
        var workers = await _masterContext.Workers
            .Where(w => w.TenantId == tenantId.Value)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Mapear a DTOs
        return workers.Select(w => new WorkerDto
        {
            Id = w.Id,
            TenantId = w.TenantId,
            Name = w.Name,
            Description = w.Description,
            IpAddress = w.IpAddress,
            MacAddress = w.MacAddress,
            Hostname = w.Hostname,
            OsInfo = w.OsInfo,
            Status = w.Status.ToString(),
            LastHeartbeat = w.LastHeartbeat,
            LastJobExecution = w.LastJobExecution,
            SupportedDatabaseTypes = w.SupportedDatabaseTypes,
            MaxConcurrentJobs = w.MaxConcurrentJobs,
            CurrentActiveJobs = w.CurrentActiveJobs,
            Version = w.Version,
            TotalBackupsProcessed = w.TotalBackupsProcessed,
            TotalBytesProcessed = w.TotalBytesProcessed,
            IsActive = w.IsActive,
            Tags = w.Tags,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
    }
}
