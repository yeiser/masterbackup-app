using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record AcceptInvitationCommand(
    string Token,
    string Password,
    string FirstName,
    string LastName,
    bool EnableTwoFactor
) : IRequest<AuthResponseDto>;
