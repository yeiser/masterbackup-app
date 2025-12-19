using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public record GetTenantQuery(Guid TenantId) : IRequest<TenantDto?>;
