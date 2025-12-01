using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class UpdateWorkerHeartbeatCommandHandler : IRequestHandler<UpdateWorkerHeartbeatCommand, Unit>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;

    public UpdateWorkerHeartbeatCommandHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(UpdateWorkerHeartbeatCommand request, CancellationToken cancellationToken)
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

        // 3. Actualizar estado y heartbeat
        worker.LastHeartbeat = DateTime.UtcNow;
        worker.CurrentActiveJobs = request.CurrentActiveJobs;
        worker.UpdatedAt = DateTime.UtcNow;

        // Parsear el estado
        if (Enum.TryParse<WorkerStatus>(request.Status, true, out var status))
        {
            worker.Status = status;
        }

        // Actualizar contadores si se proporcionan
        if (request.TotalBackupsProcessed.HasValue)
        {
            worker.TotalBackupsProcessed = request.TotalBackupsProcessed.Value;
        }

        if (request.TotalBytesProcessed.HasValue)
        {
            worker.TotalBytesProcessed = request.TotalBytesProcessed.Value;
        }

        // Actualizar LastJobExecution si hay jobs activos o acaba de terminar
        if (request.CurrentActiveJobs > 0 || (worker.CurrentActiveJobs > request.CurrentActiveJobs))
        {
            worker.LastJobExecution = DateTime.UtcNow;
        }

        await _masterContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
