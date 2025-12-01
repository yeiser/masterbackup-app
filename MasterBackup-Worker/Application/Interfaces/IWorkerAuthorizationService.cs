using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Application.Interfaces;

/// <summary>
/// Interface for validating if a worker is authorized to process a job
/// </summary>
public interface IWorkerAuthorizationService
{
    /// <summary>
    /// Validates if the current worker can process a test connection job
    /// </summary>
    /// <param name="message">The test connection message</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> CanProcessTestConnectionAsync(TestConnectionMessage message);
    
    /// <summary>
    /// Validates if the current worker can process a backup job
    /// </summary>
    /// <param name="message">The backup job message</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> CanProcessBackupJobAsync(BackupJobMessage message);
    
    /// <summary>
    /// Gets a detailed reason why the worker cannot process the job
    /// </summary>
    /// <returns>Reason for authorization failure</returns>
    string GetAuthorizationFailureReason();
}
