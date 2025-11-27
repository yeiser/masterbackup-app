using MediatR;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record Disable2FACommand(string UserId) : IRequest<bool>;
