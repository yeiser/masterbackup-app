using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public record GetApiKeyLogsQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20
) : IRequest<ApiKeyLogsResponseDto>;
