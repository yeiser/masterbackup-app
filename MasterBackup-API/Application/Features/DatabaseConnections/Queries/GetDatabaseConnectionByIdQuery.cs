using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.DatabaseConnections.Queries;

public record GetDatabaseConnectionByIdQuery(Guid Id) : IRequest<DatabaseConnectionDto>;
