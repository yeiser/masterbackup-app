using System.ComponentModel.DataAnnotations;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Common.DTOs;

public class InviteUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}
