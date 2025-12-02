using MediatR;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Handler para previsualizar las próximas ejecuciones de una expresión CRON
/// </summary>
public class PreviewCronExecutionsQueryHandler : IRequestHandler<PreviewCronExecutionsQuery, CronExecutionPreviewDto>
{
    private readonly IBackupSchedulerService _schedulerService;

    public PreviewCronExecutionsQueryHandler(IBackupSchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public async Task<CronExecutionPreviewDto> Handle(PreviewCronExecutionsQuery request, CancellationToken cancellationToken)
    {
        var result = new CronExecutionPreviewDto
        {
            CronExpression = request.CronExpression,
            TimeZone = request.TimeZone
        };

        try
        {
            // Validar expresión CRON
            result.IsValid = _schedulerService.IsValidCronExpression(request.CronExpression);
            
            if (result.IsValid)
            {
                // Calcular próximas ejecuciones
                result.NextExecutions = await _schedulerService.GetNextExecutionsAsync(
                    request.CronExpression,
                    request.TimeZone,
                    request.Count);
                
                // Generar descripción simple
                result.Description = GenerateCronDescription(request.CronExpression);
            }
            else
            {
                result.ErrorMessage = "Invalid CRON expression format. " +
                    "Use format: 'seconds minutes hours dayOfMonth month dayOfWeek [year]'. " +
                    "Example: '0 0 2 * * ?' for daily at 2:00 AM.";
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Error parsing CRON expression: {ex.Message}";
        }

        return result;
    }

    private string GenerateCronDescription(string cronExpression)
    {
        // Descripción simple basada en patrones comunes
        return cronExpression switch
        {
            "0 0 * * * ?" => "Every hour",
            "0 0 0 * * ?" => "Daily at midnight",
            "0 0 2 * * ?" => "Daily at 2:00 AM",
            "0 0 0 * * SUN" => "Weekly on Sundays at midnight",
            "0 0 0 1 * ?" => "Monthly on the 1st at midnight",
            _ => "Custom CRON schedule"
        };
    }
}
