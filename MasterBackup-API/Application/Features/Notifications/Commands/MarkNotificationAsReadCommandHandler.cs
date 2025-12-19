using MediatR;
using Microsoft.EntityFrameworkCore;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Notifications.Commands;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Unit>
{
    private readonly TenantDbContext _context;

    public MarkNotificationAsReadCommandHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId, cancellationToken);

        if (notification == null)
            throw new InvalidOperationException("Notification not found");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
