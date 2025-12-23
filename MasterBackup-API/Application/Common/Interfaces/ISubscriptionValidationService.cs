namespace MasterBackup_API.Application.Common.Interfaces;

/// <summary>
/// Servicio para validar límites de suscripción del tenant
/// </summary>
public interface ISubscriptionValidationService
{
    /// <summary>
    /// Verifica si el tenant puede crear una nueva base de datos
    /// </summary>
    Task<(bool CanCreate, string? ErrorMessage)> CanCreateDatabaseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si el tenant puede crear un nuevo usuario
    /// </summary>
    Task<(bool CanCreate, string? ErrorMessage)> CanCreateUserAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si el tenant puede crear un nuevo backup programado
    /// </summary>
    Task<(bool CanCreate, string? ErrorMessage)> CanCreateScheduledBackupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si el tenant tiene almacenamiento disponible
    /// </summary>
    Task<(bool HasSpace, string? ErrorMessage, decimal UsedGB, long MaxGB)> CheckStorageAvailabilityAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica si el tenant tiene una característica habilitada
    /// </summary>
    Task<bool> HasFeatureAsync(string featureName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene el plan actual del tenant
    /// </summary>
    Task<string?> GetCurrentPlanNameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene los límites del plan actual
    /// </summary>
    Task<(int MaxDatabases, int MaxUsers, long MaxStorageGB, bool ScheduledBackupsEnabled, bool ApiAccessEnabled)> GetCurrentLimitsAsync(CancellationToken cancellationToken = default);
}
