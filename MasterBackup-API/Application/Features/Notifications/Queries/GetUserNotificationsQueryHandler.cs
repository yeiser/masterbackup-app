using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Notifications.Queries;

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, List<NotificationDto>>
{
    private readonly TenantDbContext _context;

    public GetUserNotificationsQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == request.UserId)
            .AsQueryable();

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        // Excluir notificaciones expiradas
        query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                RedirectUrl = n.RedirectUrl,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt,
                ExpiresAt = n.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
}
