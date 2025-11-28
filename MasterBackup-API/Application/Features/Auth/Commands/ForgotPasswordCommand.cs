using MediatR;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<(bool Success, bool EmailFound)>;
