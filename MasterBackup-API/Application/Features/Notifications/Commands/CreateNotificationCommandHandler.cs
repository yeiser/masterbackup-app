using MediatR;
using Microsoft.Extensions.Logging;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Persistence;

namespace MasterBackup_API.Application.Features.Notifications.Commands;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly TenantDbContext _context;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        TenantDbContext context,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.Data.UserId,
            Type = request.Data.Type,
            Title = request.Data.Title,
            Message = request.Data.Message,
            RedirectUrl = request.Data.RedirectUrl,
            RelatedEntityId = request.Data.RelatedEntityId,
            RelatedEntityType = request.Data.RelatedEntityType,
            Metadata = request.Data.Metadata,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.Data.ExpiresAt
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification created for user {UserId}: {Title}", 
            notification.UserId, notification.Title);

        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            RedirectUrl = notification.RedirectUrl,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt,
            ExpiresAt = notification.ExpiresAt
        };
    }
}
