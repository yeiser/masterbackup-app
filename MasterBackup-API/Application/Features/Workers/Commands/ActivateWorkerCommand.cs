using MediatR;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class ActivateWorkerCommand : IRequest<Unit>
{
    public Guid WorkerId { get; set; }
}
