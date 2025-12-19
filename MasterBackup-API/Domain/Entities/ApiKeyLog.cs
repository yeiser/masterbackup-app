using System.ComponentModel.DataAnnotations;

namespace MasterBackup_API.Domain.Entities;

public class ApiKeyLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(45)]
    public string RemoteIp { get; set; } = string.Empty;

    [Required]
    public bool Success { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [MaxLength(255)]
    public string? Endpoint { get; set; }

    [MaxLength(10)]
    public string? Method { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
