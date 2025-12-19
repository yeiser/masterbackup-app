using MediatR;

namespace MasterBackup_API.Application.Features.Notifications.Commands;

public class MarkNotificationAsReadCommand : IRequest<Unit>
{
    public Guid NotificationId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
