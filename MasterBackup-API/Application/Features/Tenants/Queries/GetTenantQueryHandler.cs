using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDto?>
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<GetTenantQueryHandler> _logger;

    public GetTenantQueryHandler(
        MasterDbContext masterContext,
        ILogger<GetTenantQueryHandler> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    public async Task<TenantDto?> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _masterContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == request.TenantId && t.IsActive, cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found: {TenantId}", request.TenantId);
                return null;
            }

            return new TenantDto
            {
                Id = tenant.Id.ToString(),
                Name = tenant.Name,
                Identificacion = tenant.Identificacion,
                TipoId = tenant.TipoId,
                Direccion = tenant.Direccion,
                Telefono = tenant.Telefono,
                Email = tenant.Email,
                PaginaWeb = tenant.PaginaWeb,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt,
                IsActive = tenant.IsActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant: {TenantId}", request.TenantId);
            return null;
        }
    }
}
