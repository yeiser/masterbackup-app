using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class ActivateWorkerCommandHandler : IRequestHandler<ActivateWorkerCommand, Unit>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantContext _tenantContext;

    public ActivateWorkerCommandHandler(
        MasterDbContext masterContext,
        ITenantContext tenantContext)
    {
        _masterContext = masterContext;
        _tenantContext = tenantContext;
    }

    public async Task<Unit> Handle(ActivateWorkerCommand request, CancellationToken cancellationToken)
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

        // 3. Activar worker
        worker.IsActive = true;
        worker.UpdatedAt = DateTime.UtcNow;

        await _masterContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
