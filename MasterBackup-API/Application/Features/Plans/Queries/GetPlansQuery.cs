using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Plans.Queries;

public class GetPlansQuery : IRequest<List<PlanDto>>
{
    public bool ActiveOnly { get; set; } = true;
}
