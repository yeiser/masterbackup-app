using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Notifications.Queries;

public class GetUserNotificationsQuery : IRequest<List<NotificationDto>>
{
    public string UserId { get; set; } = string.Empty;
    public bool? IsRead { get; set; }
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}
