using MediatR;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class DeactivateWorkerCommand : IRequest<Unit>
{
    public Guid WorkerId { get; set; }
}
