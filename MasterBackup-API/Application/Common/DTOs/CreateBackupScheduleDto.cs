namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// DTO para crear un nuevo schedule de backup
/// </summary>
public class CreateBackupScheduleDto
{
    /// <summary>
    /// ID de la conexión de base de datos a respaldar
    /// </summary>
    public Guid DatabaseConnectionId { get; set; }

    /// <summary>
    /// Nombre descriptivo del schedule (requerido)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del schedule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Expresión CRON para la programación (requerido)
    /// Ejemplos:
    /// - "0 2 * * *" = Diario a las 2:00 AM
    /// - "0 0 * * 0" = Semanal los domingos a medianoche
    /// - "0 0 1 * *" = Mensual el día 1 a medianoche
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria para la expresión CRON (default: UTC)
    /// Ejemplos: "UTC", "America/Santo_Domingo", "America/New_York"
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Días de retención de los backups (default: 30)
    /// Los backups más antiguos se eliminarán automáticamente
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Indica si el schedule debe activarse inmediatamente (default: true)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Número máximo de reintentos en caso de fallo (default: 3)
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Tiempo máximo de ejecución en minutos (default: 30)
    /// El backup se cancelará si excede este tiempo
    /// </summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Prioridad de ejecución (0-10, default: 5)
    /// Mayor número = mayor prioridad en la cola
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Notificar al completar el backup (default: true)
    /// </summary>
    public bool NotifyOnCompletion { get; set; } = true;

    /// <summary>
    /// Notificar solo en caso de fallo (default: false)
    /// Si es true, sobrescribe NotifyOnCompletion
    /// </summary>
    public bool NotifyOnlyOnFailure { get; set; } = false;
}
