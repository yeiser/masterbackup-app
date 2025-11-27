using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Application.Common.DTOs;

public class AcceptInvitationDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public bool EnableTwoFactor { get; set; }
}
