using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Subscriptions.Commands;

public class CreateSubscriptionCommand : IRequest<SubscriptionDto>
{
    public Guid PlanId { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public bool UseTrial { get; set; }
}
