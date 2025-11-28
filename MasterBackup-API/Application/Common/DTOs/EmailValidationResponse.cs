namespace MasterBackup_API.Application.Common.DTOs;

public class EmailValidationResponse
{
    public bool Exists { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
