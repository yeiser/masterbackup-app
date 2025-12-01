using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class UpdateWorkerHeartbeatCommand : IRequest<Unit>
{
    public Guid WorkerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentActiveJobs { get; set; }
    public long? TotalBackupsProcessed { get; set; }
    public long? TotalBytesProcessed { get; set; }
}
