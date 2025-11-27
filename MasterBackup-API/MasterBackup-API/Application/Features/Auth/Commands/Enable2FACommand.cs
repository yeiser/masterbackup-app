using MediatR;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record Enable2FACommand(string UserId) : IRequest<bool>;
