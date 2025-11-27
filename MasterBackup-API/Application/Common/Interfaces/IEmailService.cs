namespace MasterBackup_API.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendTwoFactorCodeAsync(string email, string code);
    Task SendPasswordResetEmailAsync(string email, string resetToken);
    Task SendInvitationEmailAsync(string email, string invitationToken, string inviterName);
    Task SendWelcomeEmailAsync(string email, string name);
}
