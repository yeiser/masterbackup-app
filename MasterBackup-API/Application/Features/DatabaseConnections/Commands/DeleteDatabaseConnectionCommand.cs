using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public record DeleteDatabaseConnectionCommand(Guid Id) : IRequest<bool>;
