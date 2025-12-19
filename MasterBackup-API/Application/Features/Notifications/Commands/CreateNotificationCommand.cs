using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Notifications.Commands;

public class CreateNotificationCommand : IRequest<NotificationDto>
{
    public Guid TenantId { get; set; }
    public CreateNotificationDto Data { get; set; } = null!;
}
