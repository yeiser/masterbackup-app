using System.ComponentModel.DataAnnotations;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class UserInvitation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [Required]
    public string InvitationToken { get; set; } = string.Empty;

    public string InvitedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
