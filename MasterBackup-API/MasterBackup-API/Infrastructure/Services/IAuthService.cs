using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Infrastructure.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<bool> InviteUserAsync(InviteUserDto inviteUserDto, string invitedByUserId, Guid tenantId);
    Task<AuthResponseDto> AcceptInvitationAsync(AcceptInvitationDto acceptInvitationDto);
    Task<bool> Enable2FAAsync(string userId);
    Task<bool> Disable2FAAsync(string userId);
}
