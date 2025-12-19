using MasterBackup_API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterBackup_API.Application.Features.Tenants.Queries;

public class GetTenantApiKeyQueryHandler : IRequestHandler<GetTenantApiKeyQuery, string?>
{
    private readonly MasterDbContext _context;
    private readonly ILogger<GetTenantApiKeyQueryHandler> _logger;

    public GetTenantApiKeyQueryHandler(
        MasterDbContext context,
        ILogger<GetTenantApiKeyQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> Handle(GetTenantApiKeyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _context.Tenants
                .Where(t => t.Id == request.TenantId && t.IsActive)
                .Select(t => t.ApiKey)
                .FirstOrDefaultAsync(cancellationToken);

            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key for tenant {TenantId}", request.TenantId);
            return null;
        }
    }
}
