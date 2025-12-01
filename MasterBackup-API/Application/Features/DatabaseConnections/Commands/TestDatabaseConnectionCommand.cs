using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public record TestDatabaseConnectionCommand(Guid ConnectionId) : IRequest<bool>;
