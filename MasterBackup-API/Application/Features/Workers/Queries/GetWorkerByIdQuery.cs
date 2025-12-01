using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Workers.Queries;

public class GetWorkerByIdQuery : IRequest<WorkerDto>
{
    public Guid WorkerId { get; set; }
}
