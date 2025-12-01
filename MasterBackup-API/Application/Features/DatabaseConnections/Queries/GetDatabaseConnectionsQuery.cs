using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Queries;

public record GetDatabaseConnectionsQuery : IRequest<List<DatabaseConnectionDto>>;
