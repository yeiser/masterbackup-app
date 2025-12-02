using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Domain.Entities;

/// <summary>
/// Programación de backups automáticos para una base de datos específica
/// NOTA: Todos los backups se generan en formato directory y se comprimen automáticamente antes de subir a Azure
/// </summary>
public class BackupSchedule
{
    // ============================================
    // IDENTIFICACIÓN Y RELACIONES
    // ============================================
    
    /// <summary>
    /// ID único del schedule (Primary Key)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID del tenant propietario (multi-tenancy)
    /// IMPORTANTE: Siempre debe coincidir con DatabaseConnection.TenantId
    /// </summary>
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// ID de la conexión de base de datos a respaldar
    /// Foreign Key → DatabaseConnections.Id
    /// </summary>
    public Guid DatabaseConnectionId { get; set; }
    
    /// <summary>
    /// Relación de navegación hacia DatabaseConnection
    /// La asignación de workers se maneja a través de DatabaseConnection
    /// </summary>
    public DatabaseConnection DatabaseConnection { get; set; } = null!;
    
    // ============================================
    // CONFIGURACIÓN DE PROGRAMACIÓN
    // ============================================
    
    /// <summary>
    /// Nombre descriptivo del schedule
    /// Ejemplo: "Backup Diario Producción", "Respaldo Semanal Domingo"
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción opcional del propósito del backup
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Expresión CRON que define la frecuencia de ejecución
    /// Formato: "Segundos Minutos Horas DíaMes Mes DíaSemana [Año]"
    /// Ejemplos:
    /// - "0 0 2 * * ?" → Diario a las 2:00 AM
    /// - "0 0 3 * * SUN" → Domingos a las 3:00 AM
    /// - "0 0 1 1 * ?" → Primer día de cada mes a la 1:00 AM
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;
    
    /// <summary>
    /// Zona horaria para interpretar la expresión CRON
    /// Ejemplo: "America/New_York", "Europe/Madrid", "UTC"
    /// Default: UTC
    /// </summary>
    public string TimeZone { get; set; } = "UTC";
    
    // ============================================
    // RETENCIÓN
    // ============================================
    
    /// <summary>
    /// Días de retención del backup antes de eliminarse automáticamente
    /// Ejemplo: 30 = El backup se elimina después de 30 días
    /// </summary>
    public int RetentionDays { get; set; }
    
    // ============================================
    // ESTADO Y SEGUIMIENTO
    // ============================================
    
    /// <summary>
    /// Indica si el schedule está activo
    /// true = Job registrado en Quartz.NET, se ejecutará según CRON
    /// false = Job pausado, no se ejecutará
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Fecha y hora de la próxima ejecución programada (calculada por Quartz)
    /// Se actualiza automáticamente después de cada ejecución
    /// Null si el schedule está inactivo
    /// </summary>
    public DateTime? NextRun { get; set; }
    
    /// <summary>
    /// Fecha y hora de la última ejecución completada
    /// Se actualiza cuando BackupExecution cambia a Status=Completed o Failed
    /// </summary>
    public DateTime? LastRun { get; set; }
    
    /// <summary>
    /// Estado de la última ejecución
    /// Valores: Pending, InProgress, Completed, Failed, Cancelled, Timeout
    /// </summary>
    public BackupStatus? LastExecutionStatus { get; set; }
    
    /// <summary>
    /// Mensaje de error de la última ejecución (si falló)
    /// </summary>
    public string? LastExecutionError { get; set; }
    
    // ============================================
    // OPCIONES AVANZADAS
    // ============================================
    
    /// <summary>
    /// Número máximo de reintentos en caso de fallo
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Minutos de timeout para la ejecución del backup
    /// Default: 30 minutos
    /// </summary>
    public int TimeoutMinutes { get; set; } = 30;
    
    /// <summary>
    /// Prioridad del job en la cola de RabbitMQ
    /// Valores: 0 (baja) a 10 (alta)
    /// </summary>
    public int Priority { get; set; } = 5;
    
    /// <summary>
    /// Indica si se debe notificar al usuario al completar
    /// </summary>
    public bool NotifyOnCompletion { get; set; } = true;
    
    /// <summary>
    /// Indica si se debe notificar solo en caso de fallo
    /// </summary>
    public bool NotifyOnlyOnFailure { get; set; } = false;
    
    // ============================================
    // AUDITORÍA
    // ============================================
    
    /// <summary>
    /// Fecha de creación del schedule
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Usuario que creó el schedule
    /// </summary>
    public Guid CreatedBy { get; set; }
    
    /// <summary>
    /// Fecha de última modificación
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Usuario que modificó por última vez
    /// </summary>
    public Guid? UpdatedBy { get; set; }
    
    // ============================================
    // COLECCIONES DE NAVEGACIÓN
    // ============================================
    
    /// <summary>
    /// Historial de todas las ejecuciones generadas por este schedule
    /// TODO: Descomentar cuando se cree la entidad BackupExecution
    /// </summary>
    // public ICollection<BackupExecution> BackupExecutions { get; set; } = new List<BackupExecution>();
}
