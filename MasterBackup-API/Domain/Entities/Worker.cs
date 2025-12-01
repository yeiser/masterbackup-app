using System.ComponentModel.DataAnnotations;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class Worker
{
    [Key]
    public Guid Id { get; set; }
    
    // Relación con Tenant - Worker pertenece a UN solo tenant
    [Required]
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    // Identificación del Worker
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Información de Red y Sistema
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    [MaxLength(50)]
    public string? MacAddress { get; set; }
    
    [MaxLength(100)]
    public string? Hostname { get; set; }
    
    [MaxLength(200)]
    public string? OsInfo { get; set; }
    
    // Estado y Salud
    [Required]
    public WorkerStatus Status { get; set; } = WorkerStatus.Online;
    
    [Required]
    public DateTime LastHeartbeat { get; set; }
    
    public DateTime? LastJobExecution { get; set; }
    
    // Capacidades y Configuración
    [Required]
    public string[] SupportedDatabaseTypes { get; set; } = Array.Empty<string>();
    
    [Required]
    public int MaxConcurrentJobs { get; set; } = 1;
    
    [Required]
    public int CurrentActiveJobs { get; set; } = 0;
    
    // Tags para matching automático
    [Required]
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    // Metadata del Worker
    [MaxLength(20)]
    public string? Version { get; set; }
    
    public long TotalBackupsProcessed { get; set; } = 0;
    
    public long TotalBytesProcessed { get; set; } = 0;
    
    // Control
    [Required]
    public bool IsActive { get; set; } = true;
    
    // Auditoría
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public Guid? CreatedBy { get; set; }
}
