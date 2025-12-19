using MediatR;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public record GetTenantApiKeyQuery(
    Guid TenantId
) : IRequest<string?>;
