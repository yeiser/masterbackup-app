using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public class GetApiKeyLogsQueryHandler : IRequestHandler<GetApiKeyLogsQuery, ApiKeyLogsResponseDto>
{
    private readonly MasterDbContext _context;
    private readonly ILogger<GetApiKeyLogsQueryHandler> _logger;

    public GetApiKeyLogsQueryHandler(
        MasterDbContext context,
        ILogger<GetApiKeyLogsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiKeyLogsResponseDto> Handle(GetApiKeyLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.ApiKeyLogs
                .Where(log => log.TenantId == request.TenantId)
                .OrderByDescending(log => log.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var logs = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(log => new ApiKeyLogDto
                {
                    Id = log.Id,
                    RemoteIp = log.RemoteIp,
                    Success = log.Success,
                    ErrorMessage = log.ErrorMessage,
                    Endpoint = log.Endpoint,
                    Method = log.Method,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new ApiKeyLogsResponseDto
            {
                Logs = logs,
                TotalCount = totalCount,
                PageSize = request.PageSize,
                CurrentPage = request.Page,
                HasMore = totalCount > request.Page * request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key logs for tenant {TenantId}", request.TenantId);
            return new ApiKeyLogsResponseDto();
        }
    }
}
