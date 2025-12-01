using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public record UpdateTestResultCommand : IRequest<bool>
{
    public Guid ConnectionId { get; set; }
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ServerVersion { get; set; }
}
