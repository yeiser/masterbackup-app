using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Notifications.Queries;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly TenantDbContext _context;

    public GetUnreadCountQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(n => n.UserId == request.UserId && !n.IsRead)
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow)
            .CountAsync(cancellationToken);
    }
}
