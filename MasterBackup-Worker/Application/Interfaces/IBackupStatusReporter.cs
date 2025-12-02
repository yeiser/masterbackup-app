namespace MasterBackup_Worker.Application.Interfaces;

/// <summary>
/// Service for reporting backup status to API
/// </summary>
public interface IBackupStatusReporter
{
    /// <summary>
    /// Report that backup has started
    /// </summary>
    Task ReportStartedAsync(Guid jobId, Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Report backup progress
    /// </summary>
    Task ReportProgressAsync(
        Guid jobId,
        Guid tenantId,
        int progressPercentage,
        string currentStep,
        long processedBytes,
        long totalBytes,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Report backup completed successfully
    /// </summary>
    Task ReportCompletedAsync(
        Guid jobId,
        Guid tenantId,
        string blobUrl,
        string blobName,
        long backupSizeBytes,
        string compressionType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Report backup failed
    /// </summary>
    Task ReportFailedAsync(
        Guid jobId,
        Guid tenantId,
        string errorMessage,
        string? errorCode = null,
        string? stackTrace = null,
        int retryCount = 0,
        bool willRetry = false,
        DateTime? nextRetryAt = null,
        CancellationToken cancellationToken = default);
}
