using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Subscriptions.Queries;

public class GetCurrentSubscriptionQuery : IRequest<SubscriptionDto?>
{
}
