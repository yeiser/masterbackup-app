using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Queries;

public class GetWorkerStatsQueryHandler : IRequestHandler<GetWorkerStatsQuery, WorkerStatsDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;

    public GetWorkerStatsQueryHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
    }

    public async Task<WorkerStatsDto> Handle(GetWorkerStatsQuery request, CancellationToken cancellationToken)
    {
        // 1. Validar que el tenant esté autenticado
        var tenantId = _tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            throw new UnauthorizedAccessException("No se pudo identificar el tenant");
        }

        // 2. Obtener estadísticas de workers del tenant
        var workers = await _masterContext.Workers
            .Where(w => w.TenantId == tenantId.Value)
            .ToListAsync(cancellationToken);

        // 3. Calcular estadísticas
        var stats = new WorkerStatsDto
        {
            TotalWorkers = workers.Count,
            OnlineWorkers = workers.Count(w => w.Status == WorkerStatus.Online),
            OfflineWorkers = workers.Count(w => w.Status == WorkerStatus.Offline),
            BusyWorkers = workers.Count(w => w.Status == WorkerStatus.Busy),
            TotalBackupsProcessed = workers.Sum(w => w.TotalBackupsProcessed),
            TotalBytesProcessed = workers.Sum(w => w.TotalBytesProcessed)
        };

        return stats;
    }
}
