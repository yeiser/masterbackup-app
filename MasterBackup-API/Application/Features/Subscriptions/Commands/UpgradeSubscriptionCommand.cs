using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class UpgradeSubscriptionCommand : IRequest<SubscriptionDto>
{
    public Guid NewPlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
}
