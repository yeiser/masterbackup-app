using MasterBackup_API.Domain.Entities;

namespace MasterBackup_API.Application.Common.Interfaces;

/// <summary>
/// Servicio para gestionar la programación de backups con Quartz.NET
/// </summary>
public interface IBackupSchedulerService
{
    /// <summary>
    /// Programa un backup automático basado en un schedule
    /// </summary>
    /// <param name="backupSchedule">Schedule a programar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task ScheduleBackupAsync(BackupSchedule backupSchedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reprograma un backup existente (útil cuando cambia el CRON)
    /// </summary>
    /// <param name="backupSchedule">Schedule actualizado</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task RescheduleBackupAsync(BackupSchedule backupSchedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desprograma un backup (elimina el job de Quartz)
    /// </summary>
    /// <param name="backupScheduleId">ID del schedule a desprogramar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task UnscheduleBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pausa un backup programado
    /// </summary>
    /// <param name="backupScheduleId">ID del schedule a pausar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task PauseBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reanuda un backup pausado
    /// </summary>
    /// <param name="backupScheduleId">ID del schedule a reanudar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task ResumeBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la próxima fecha de ejecución de un backup
    /// </summary>
    /// <param name="backupScheduleId">ID del schedule</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Fecha de próxima ejecución o null si no está programado</returns>
    Task<DateTime?> GetNextFireTimeAsync(Guid backupScheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si una expresión CRON es válida
    /// </summary>
    /// <param name="cronExpression">Expresión CRON a validar</param>
    /// <returns>True si es válida, false si no</returns>
    bool IsValidCronExpression(string cronExpression);

    /// <summary>
    /// Calcula las próximas N ejecuciones de una expresión CRON
    /// </summary>
    /// <param name="cronExpression">Expresión CRON</param>
    /// <param name="timeZone">Zona horaria</param>
    /// <param name="count">Número de ejecuciones a calcular</param>
    /// <returns>Lista de fechas de ejecución</returns>
    Task<List<DateTime>> GetNextExecutionsAsync(string cronExpression, string timeZone, int count = 5);
}
