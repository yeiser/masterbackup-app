namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// DTO para actualizar un schedule de backup existente
/// </summary>
public class UpdateBackupScheduleDto
{
    /// <summary>
    /// Nombre descriptivo del schedule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del schedule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Expresión CRON para la programación
    /// Ejemplos:
    /// - "0 2 * * *" = Diario a las 2:00 AM
    /// - "0 0 * * 0" = Semanal los domingos a medianoche
    /// - "0 0 1 * *" = Mensual el día 1 a medianoche
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria para la expresión CRON
    /// Ejemplos: "UTC", "America/Santo_Domingo", "America/New_York"
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Días de retención de los backups
    /// Los backups más antiguos se eliminarán automáticamente
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Número máximo de reintentos en caso de fallo
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Tiempo máximo de ejecución en minutos
    /// El backup se cancelará si excede este tiempo
    /// </summary>
    public int TimeoutMinutes { get; set; }

    /// <summary>
    /// Prioridad de ejecución (0-10)
    /// Mayor número = mayor prioridad en la cola
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Notificar al completar el backup
    /// </summary>
    public bool NotifyOnCompletion { get; set; }

    /// <summary>
    /// Notificar solo en caso de fallo
    /// Si es true, sobrescribe NotifyOnCompletion
    /// </summary>
    public bool NotifyOnlyOnFailure { get; set; }

    /// <summary>
    /// Restaurar automáticamente el backup después de completarse
    /// </summary>
    public bool AutoRestore { get; set; }
}
