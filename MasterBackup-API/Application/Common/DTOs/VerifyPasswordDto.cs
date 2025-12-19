using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Application.Common.DTOs;

public class VerifyPasswordDto
{
    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}
