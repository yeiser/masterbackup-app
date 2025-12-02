namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// DTO para previsualizar las próximas ejecuciones de una expresión CRON
/// </summary>
public class CronExecutionPreviewDto
{
    /// <summary>
    /// Expresión CRON evaluada
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// Zona horaria utilizada
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Indica si la expresión CRON es válida
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Mensaje de error si la expresión no es válida
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Descripción legible de la expresión CRON
    /// Ejemplo: "Cada día a las 2:00 AM"
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Lista de las próximas 5-10 fechas de ejecución
    /// </summary>
    public List<DateTime> NextExecutions { get; set; } = new();
}
