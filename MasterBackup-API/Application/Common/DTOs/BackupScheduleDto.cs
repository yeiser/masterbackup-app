using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// DTO para representar un schedule de backup en respuestas de la API
/// </summary>
public class BackupScheduleDto
{
    /// <summary>
    /// ID único del schedule
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID del tenant propietario
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// ID de la conexión de base de datos
    /// </summary>
    public Guid DatabaseConnectionId { get; set; }

    /// <summary>
    /// Nombre de la conexión de base de datos
    /// </summary>
    public string DatabaseConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo del schedule
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Expresión CRON para la programación
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria para la expresión CRON
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Días de retención de los backups
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Indica si el schedule está activo
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Próxima fecha y hora de ejecución
    /// </summary>
    public DateTime? NextRun { get; set; }

    /// <summary>
    /// Última fecha y hora de ejecución
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Estado de la última ejecución
    /// </summary>
    public BackupStatus? LastExecutionStatus { get; set; }

    /// <summary>
    /// Mensaje de error de la última ejecución (si falló)
    /// </summary>
    public string? LastExecutionError { get; set; }

    /// <summary>
    /// Número máximo de reintentos en caso de fallo
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Tiempo máximo de ejecución en minutos
    /// </summary>
    public int TimeoutMinutes { get; set; }

    /// <summary>
    /// Prioridad de ejecución (0-10, mayor = más prioritario)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Notificar al completar el backup
    /// </summary>
    public bool NotifyOnCompletion { get; set; }

    /// <summary>
    /// Notificar solo en caso de fallo
    /// </summary>
    public bool NotifyOnlyOnFailure { get; set; }

    /// <summary>
    /// Restaurar automáticamente el backup después de completarse
    /// </summary>
    public bool AutoRestore { get; set; }

    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Usuario que creó el schedule
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Usuario que actualizó el schedule
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}
