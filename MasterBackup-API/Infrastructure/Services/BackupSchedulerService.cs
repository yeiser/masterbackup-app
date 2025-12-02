using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de programación de backups usando Quartz.NET
/// </summary>
public class BackupSchedulerService : IBackupSchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<BackupSchedulerService> _logger;

    public BackupSchedulerService(
        ISchedulerFactory schedulerFactory,
        ILogger<BackupSchedulerService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Programa un nuevo backup
    /// </summary>
    public async Task ScheduleBackupAsync(BackupSchedule backupSchedule, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // Crear JobKey único para este backup schedule
        var jobKey = new JobKey($"backup-{backupSchedule.Id}", $"tenant-{backupSchedule.TenantId}");

        // Verificar si ya existe un job con este ID
        if (await scheduler.CheckExists(jobKey, cancellationToken))
        {
            _logger.LogWarning(
                "Job already exists for BackupScheduleId: {BackupScheduleId}. Skipping schedule creation.",
                backupSchedule.Id);
            return;
        }

        // Crear el JobDetail con los datos necesarios
        var job = JobBuilder.Create<BackupJob>()
            .WithIdentity(jobKey)
            .WithDescription($"Backup job for {backupSchedule.Name}")
            .UsingJobData("BackupScheduleId", backupSchedule.Id.ToString())
            .UsingJobData("DatabaseConnectionId", backupSchedule.DatabaseConnectionId.ToString())
            .UsingJobData("TenantId", backupSchedule.TenantId.ToString())
            .UsingJobData("CronExpression", backupSchedule.CronExpression)
            .StoreDurably(false) // El job se elimina cuando no tiene triggers
            .Build();

        // Crear el trigger con la expresión CRON
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{backupSchedule.Id}", $"tenant-{backupSchedule.TenantId}")
            .ForJob(jobKey)
            .WithDescription($"Trigger for {backupSchedule.Name}")
            .WithCronSchedule(
                backupSchedule.CronExpression,
                x => x.InTimeZone(GetTimeZoneInfo(backupSchedule.TimeZone))
                      .WithMisfireHandlingInstructionFireAndProceed()) // Ejecutar inmediatamente si se perdió la ejecución
            .WithPriority(backupSchedule.Priority) // Usar la prioridad del schedule
            .StartNow()
            .Build();

        // Programar el job
        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation(
            "Backup scheduled successfully. BackupScheduleId: {BackupScheduleId}, " +
            "CronExpression: {CronExpression}, TimeZone: {TimeZone}, Priority: {Priority}, " +
            "NextFireTime: {NextFireTime}",
            backupSchedule.Id, backupSchedule.CronExpression, backupSchedule.TimeZone,
            backupSchedule.Priority, trigger.GetNextFireTimeUtc());
    }

    /// <summary>
    /// Reprograma un backup existente
    /// </summary>
    public async Task RescheduleBackupAsync(BackupSchedule backupSchedule, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey($"trigger-{backupSchedule.Id}", $"tenant-{backupSchedule.TenantId}");

        // Verificar si existe el trigger
        if (!await scheduler.CheckExists(triggerKey, cancellationToken))
        {
            _logger.LogWarning(
                "Trigger not found for BackupScheduleId: {BackupScheduleId}. Scheduling new job instead.",
                backupSchedule.Id);
            await ScheduleBackupAsync(backupSchedule, cancellationToken);
            return;
        }

        // Crear nuevo trigger con la expresión CRON actualizada
        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob($"backup-{backupSchedule.Id}", $"tenant-{backupSchedule.TenantId}")
            .WithDescription($"Trigger for {backupSchedule.Name}")
            .WithCronSchedule(
                backupSchedule.CronExpression,
                x => x.InTimeZone(GetTimeZoneInfo(backupSchedule.TimeZone))
                      .WithMisfireHandlingInstructionFireAndProceed())
            .WithPriority(backupSchedule.Priority)
            .StartNow()
            .Build();

        // Reprogramar el trigger
        await scheduler.RescheduleJob(triggerKey, newTrigger, cancellationToken);

        _logger.LogInformation(
            "Backup rescheduled successfully. BackupScheduleId: {BackupScheduleId}, " +
            "NewCronExpression: {CronExpression}, NewTimeZone: {TimeZone}, " +
            "NextFireTime: {NextFireTime}",
            backupSchedule.Id, backupSchedule.CronExpression, backupSchedule.TimeZone,
            newTrigger.GetNextFireTimeUtc());
    }

    /// <summary>
    /// Desprograma un backup
    /// </summary>
    public async Task UnscheduleBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        // Buscar el job en todos los grupos de tenants
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var jobKey = jobKeys.FirstOrDefault(k => k.Name == $"backup-{backupScheduleId}");

        if (jobKey == null)
        {
            _logger.LogWarning(
                "Job not found for BackupScheduleId: {BackupScheduleId}. Already unscheduled?",
                backupScheduleId);
            return;
        }

        // Eliminar el job (esto también elimina todos sus triggers)
        var deleted = await scheduler.DeleteJob(jobKey, cancellationToken);

        if (deleted)
        {
            _logger.LogInformation(
                "Backup unscheduled successfully. BackupScheduleId: {BackupScheduleId}",
                backupScheduleId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to unschedule backup. BackupScheduleId: {BackupScheduleId}",
                backupScheduleId);
        }
    }

    /// <summary>
    /// Pausa un backup
    /// </summary>
    public async Task PauseBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        // Buscar el job
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var jobKey = jobKeys.FirstOrDefault(k => k.Name == $"backup-{backupScheduleId}");

        if (jobKey == null)
        {
            _logger.LogWarning(
                "Job not found for BackupScheduleId: {BackupScheduleId}. Cannot pause.",
                backupScheduleId);
            return;
        }

        // Pausar el job
        await scheduler.PauseJob(jobKey, cancellationToken);

        _logger.LogInformation(
            "Backup paused successfully. BackupScheduleId: {BackupScheduleId}",
            backupScheduleId);
    }

    /// <summary>
    /// Reanuda un backup pausado
    /// </summary>
    public async Task ResumeBackupAsync(Guid backupScheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        // Buscar el job
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var jobKey = jobKeys.FirstOrDefault(k => k.Name == $"backup-{backupScheduleId}");

        if (jobKey == null)
        {
            _logger.LogWarning(
                "Job not found for BackupScheduleId: {BackupScheduleId}. Cannot resume.",
                backupScheduleId);
            return;
        }

        // Reanudar el job
        await scheduler.ResumeJob(jobKey, cancellationToken);

        _logger.LogInformation(
            "Backup resumed successfully. BackupScheduleId: {BackupScheduleId}",
            backupScheduleId);
    }

    /// <summary>
    /// Obtiene la próxima fecha de ejecución
    /// </summary>
    public async Task<DateTime?> GetNextFireTimeAsync(Guid backupScheduleId, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        // Buscar el trigger
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), cancellationToken);
        var triggerKey = triggerKeys.FirstOrDefault(k => k.Name == $"trigger-{backupScheduleId}");

        if (triggerKey == null)
        {
            return null;
        }

        var trigger = await scheduler.GetTrigger(triggerKey, cancellationToken);
        var nextFireTime = trigger?.GetNextFireTimeUtc();

        return nextFireTime?.DateTime;
    }

    /// <summary>
    /// Valida si una expresión CRON es válida
    /// </summary>
    public bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        try
        {
            var expression = new CronExpression(cronExpression);
            // Intentar calcular la próxima ejecución para verificar que es válida
            var nextTime = expression.GetNextValidTimeAfter(DateTimeOffset.UtcNow);
            return nextTime.HasValue;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calcula las próximas N ejecuciones
    /// </summary>
    public async Task<List<DateTime>> GetNextExecutionsAsync(string cronExpression, string timeZone, int count = 5)
    {
        var executions = new List<DateTime>();

        try
        {
            var expression = new CronExpression(cronExpression)
            {
                TimeZone = GetTimeZoneInfo(timeZone)
            };

            var currentTime = DateTimeOffset.UtcNow;

            for (int i = 0; i < count; i++)
            {
                var nextTime = expression.GetNextValidTimeAfter(currentTime);
                if (nextTime.HasValue)
                {
                    executions.Add(nextTime.Value.DateTime);
                    currentTime = nextTime.Value;
                }
                else
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error calculating next executions for CRON: {CronExpression}",
                cronExpression);
        }

        return await Task.FromResult(executions);
    }

    #region Helper Methods

    /// <summary>
    /// Convierte un string de zona horaria a TimeZoneInfo
    /// </summary>
    private TimeZoneInfo GetTimeZoneInfo(string timeZone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch
        {
            _logger.LogWarning(
                "TimeZone '{TimeZone}' not found. Using UTC instead.",
                timeZone);
            return TimeZoneInfo.Utc;
        }
    }

    #endregion
}
