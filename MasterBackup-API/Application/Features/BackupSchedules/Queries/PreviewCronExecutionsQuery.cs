using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.BackupSchedules.Queries;

/// <summary>
/// Query para previsualizar las próximas ejecuciones de una expresión CRON
/// </summary>
public class PreviewCronExecutionsQuery : IRequest<CronExecutionPreviewDto>
{
    /// <summary>
    /// Expresión CRON a evaluar
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria para la expresión CRON
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Número de ejecuciones futuras a calcular (default: 5)
    /// </summary>
    public int Count { get; set; } = 5;
}
