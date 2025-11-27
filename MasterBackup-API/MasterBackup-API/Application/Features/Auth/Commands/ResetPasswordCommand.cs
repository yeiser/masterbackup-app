using MediatR;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest<bool>;
