using MediatR;

namespace MasterBackup_API.Application.Features.Notifications.Queries;

public class GetUnreadCountQuery : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;
}
