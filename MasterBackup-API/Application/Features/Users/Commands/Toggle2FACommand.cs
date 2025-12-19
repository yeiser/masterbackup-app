using MediatR;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record Toggle2FACommand(
    string UserId,
    bool Enable
) : IRequest<bool>;
