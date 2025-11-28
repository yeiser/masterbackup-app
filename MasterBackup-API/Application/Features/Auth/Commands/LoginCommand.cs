using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record LoginCommand(LoginDto Dto) : IRequest<AuthResponseDto>
{
    public string Email => Dto.Email;
    public string Password => Dto.Password;
    public string? TwoFactorCode => Dto.TwoFactorCode;
}
