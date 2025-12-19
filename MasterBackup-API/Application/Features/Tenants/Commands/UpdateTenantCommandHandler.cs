using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Tenants.Commands;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto?>
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        MasterDbContext masterContext,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    public async Task<TenantDto?> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
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

            // Actualizar campos
            tenant.Name = request.Dto.Name;
            tenant.Identificacion = request.Dto.Identificacion;
            tenant.TipoId = request.Dto.TipoId;
            tenant.Direccion = request.Dto.Direccion;
            tenant.Telefono = request.Dto.Telefono;
            tenant.Email = request.Dto.Email;
            tenant.PaginaWeb = request.Dto.PaginaWeb;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _masterContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant updated successfully: {TenantId}", request.TenantId);

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
            _logger.LogError(ex, "Error updating tenant: {TenantId}", request.TenantId);
            return null;
        }
    }
}
