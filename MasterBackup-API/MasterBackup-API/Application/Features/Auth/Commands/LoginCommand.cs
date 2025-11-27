using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? TwoFactorCode
) : IRequest<AuthResponseDto>;
