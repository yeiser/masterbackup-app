using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Workers.Commands;

public class RegisterWorkerCommand : IRequest<RegisterWorkerResponseDto>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Hostname { get; set; }
    public string? OsInfo { get; set; }
    public string[] SupportedDatabaseTypes { get; set; } = Array.Empty<string>();
    public int MaxConcurrentJobs { get; set; } = 1;
    public string? Version { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
