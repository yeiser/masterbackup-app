using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Commands;

public record UpdateDatabaseConnectionCommand(Guid Id, UpdateDatabaseConnectionDto Dto) : IRequest<DatabaseConnectionDto>;
