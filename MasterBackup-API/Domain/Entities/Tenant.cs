using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Domain.Entities;

public class Tenant
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ConnectionString { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Identificacion { get; set; }

    [MaxLength(2)]
    public string? TipoId { get; set; }

    [MaxLength(50)]
    public string? Direccion { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(50)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? PaginaWeb { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
