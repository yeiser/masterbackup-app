using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Application.Common.DTOs;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}
