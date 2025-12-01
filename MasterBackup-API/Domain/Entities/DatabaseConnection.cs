using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

public class DatabaseConnection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DatabaseType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string? EngineVersion { get; set; }
    public string? SSLMode { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccessful { get; set; }
    public string? LastTestStatus { get; set; }
    
    // Asignación de Worker (enfoque híbrido)
    // Nota: AssignedWorkerId referencia a un Worker en la base de datos Master
    // No hay relación de clave foránea porque están en diferentes bases de datos
    public Guid? AssignedWorkerId { get; set; }
    
    // Tags para matching automático
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    // Modo de asignación
    public WorkerAssignmentMode AssignmentMode { get; set; } = WorkerAssignmentMode.Auto;
    
    // Navegación
    public ICollection<BackupSchedule> BackupSchedules { get; set; } = new List<BackupSchedule>();
}
