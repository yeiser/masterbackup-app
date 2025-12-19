using MediatR;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record VerifyPasswordCommand(
    string UserId,
    string Password
) : IRequest<bool>;
