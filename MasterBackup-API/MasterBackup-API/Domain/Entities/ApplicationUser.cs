using Microsoft.AspNetCore.Identity;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public Guid TenantId { get; set; }

    public new bool TwoFactorEnabled { get; set; }
    public string? TwoFactorCode { get; set; }
    public DateTime? TwoFactorCodeExpiry { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
