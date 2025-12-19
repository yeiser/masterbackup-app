using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Plans.Queries;

public class GetPlanByIdQuery : IRequest<PlanDto?>
{
    public Guid PlanId { get; set; }
}
