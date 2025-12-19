using MediatR;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record ChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword
) : IRequest<bool>;
