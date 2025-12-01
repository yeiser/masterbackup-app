using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Queries;

public class GetWorkerByIdQueryHandler : IRequestHandler<GetWorkerByIdQuery, WorkerDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;

    public GetWorkerByIdQueryHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
    }

    public async Task<WorkerDto> Handle(GetWorkerByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Validar que el tenant esté autenticado
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el tenant");
        }

        // 2. Buscar el worker y validar que pertenezca al tenant
        var worker = await _masterContext.Workers
            .FirstOrDefaultAsync(w => w.Id == request.WorkerId && w.TenantId == tenantId.Value, cancellationToken);
        
        if (worker == null)
        {
            throw new KeyNotFoundException($"Worker con ID {request.WorkerId} no encontrado o no pertenece a este tenant");
        }

        // 3. Mapear a DTO
        return new WorkerDto
        {
            Id = worker.Id,
            TenantId = worker.TenantId,
            Name = worker.Name,
            Description = worker.Description,
            IpAddress = worker.IpAddress,
            MacAddress = worker.MacAddress,
            Hostname = worker.Hostname,
            OsInfo = worker.OsInfo,
            Status = worker.Status.ToString(),
            LastHeartbeat = worker.LastHeartbeat,
            LastJobExecution = worker.LastJobExecution,
            SupportedDatabaseTypes = worker.SupportedDatabaseTypes,
            MaxConcurrentJobs = worker.MaxConcurrentJobs,
            CurrentActiveJobs = worker.CurrentActiveJobs,
            Version = worker.Version,
            TotalBackupsProcessed = worker.TotalBackupsProcessed,
            TotalBytesProcessed = worker.TotalBytesProcessed,
            IsActive = worker.IsActive,
            Tags = worker.Tags,
            CreatedAt = worker.CreatedAt,
            UpdatedAt = worker.UpdatedAt
        };
    }
}
