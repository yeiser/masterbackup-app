namespace MasterBackup_API.Application.Common.DTOs;

public class Verify2FADto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TwoFactorCode { get; set; } = string.Empty;
}
