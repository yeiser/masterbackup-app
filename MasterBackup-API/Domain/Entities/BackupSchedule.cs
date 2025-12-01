namespace MasterBackup_API.Domain.Entities;

public class BackupSchedule
{
    public Guid Id { get; set; }
    public Guid DatabaseConnectionId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public int RetentionDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? NextRun { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navegación
    public DatabaseConnection DatabaseConnection { get; set; } = null!;
}
