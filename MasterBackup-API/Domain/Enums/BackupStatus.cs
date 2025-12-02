namespace MasterBackup_API.Domain.Enums;

/// <summary>
/// Estado de ejecución de un backup
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// Pendiente de ejecución (en cola)
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// En progreso (siendo ejecutado por el worker)
    /// </summary>
    InProgress = 2,
    
    /// <summary>
    /// Completado exitosamente
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// Fallido con error
    /// </summary>
    Failed = 4,
    
    /// <summary>
    /// Cancelado manualmente
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Timeout (excedió el tiempo máximo)
    /// </summary>
    Timeout = 6
}
