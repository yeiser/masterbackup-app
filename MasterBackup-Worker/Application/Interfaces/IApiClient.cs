namespace MasterBackup_Worker.Application.Interfaces;

/// <summary>
/// Interface for communicating with the MasterBackup API
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Notifies the API that a test connection has completed
    /// </summary>
    Task NotifyTestConnectionCompletedAsync(Guid connectionId, bool success, string message);
    
    /// <summary>
    /// Updates the status of a backup execution
    /// </summary>
    Task UpdateBackupExecutionStatusAsync(Guid backupExecutionId, string status, string? errorMessage = null);
    
    /// <summary>
    /// Uploads backup file metadata to the API
    /// </summary>
    Task UploadBackupFileMetadataAsync(Guid backupExecutionId, string fileName, string blobUrl, long fileSize);
    
    /// <summary>
    /// Registers the worker with the API
    /// </summary>
    Task<(bool Success, Guid WorkerId, Guid TenantId, string Message)> RegisterWorkerAsync(
        string name,
        string? hostname,
        string? osInfo,
        string[] supportedDatabaseTypes,
        int maxConcurrentJobs,
        string? version,
        string[] tags,
        Guid? existingWorkerId = null);
    
    /// <summary>
    /// Sends a heartbeat to the API
    /// </summary>
    Task SendHeartbeatAsync(
        Guid workerId,
        string status,
        int currentActiveJobs,
        long? totalBackupsProcessed = null,
        long? totalBytesProcessed = null);

    /// <summary>
    /// Gets worker credentials (RabbitMQ + Azure Storage) from the API
    /// </summary>
    Task<MasterBackup_Worker.Domain.Models.WorkerCredentialsDto> GetCredentialsAsync(CancellationToken cancellationToken = default);
}
