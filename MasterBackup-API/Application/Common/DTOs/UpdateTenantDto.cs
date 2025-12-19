using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Application.Common.DTOs;

public class UpdateTenantDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "La identificación no puede exceder 20 caracteres")]
    public string? Identificacion { get; set; }

    [MaxLength(2, ErrorMessage = "El tipo de ID no puede exceder 2 caracteres")]
    public string? TipoId { get; set; }

    [MaxLength(50, ErrorMessage = "La dirección no puede exceder 50 caracteres")]
    public string? Direccion { get; set; }

    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Telefono { get; set; }

    [MaxLength(50, ErrorMessage = "El email no puede exceder 50 caracteres")]
    [EmailAddress(ErrorMessage = "El email no es válido")]
    public string? Email { get; set; }

    [MaxLength(50, ErrorMessage = "La página web no puede exceder 50 caracteres")]
    [Url(ErrorMessage = "La página web no es una URL válida")]
    public string? PaginaWeb { get; set; }
}
