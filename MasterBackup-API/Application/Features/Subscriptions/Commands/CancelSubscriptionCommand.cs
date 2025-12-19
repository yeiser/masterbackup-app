using MediatR;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommand : IRequest<Unit>
{
    public string Reason { get; set; } = string.Empty;
}
