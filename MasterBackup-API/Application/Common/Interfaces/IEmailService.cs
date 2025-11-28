using MasterBackup_API.Domain.Entities;

namespace MasterBackup_API.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendTwoFactorCodeAsync(ApplicationUser user, string code);
    Task SendPasswordResetEmailAsync(ApplicationUser user, string resetToken);
    Task SendInvitationEmailAsync(ApplicationUser user, string invitationToken, string inviterName);
    Task SendWelcomeEmailAsync(ApplicationUser user);
}
