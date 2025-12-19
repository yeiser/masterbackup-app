using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Tenants.Commands;

public record UpdateTenantCommand(Guid TenantId, UpdateTenantDto Dto) : IRequest<TenantDto?>;
