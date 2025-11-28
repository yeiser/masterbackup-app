using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Common.DTOs;

public class InviteUserDto
{
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
