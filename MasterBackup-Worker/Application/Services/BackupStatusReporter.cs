using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Application.Services;

public class BackupStatusReporter : IBackupStatusReporter
{
    private readonly HttpClient _httpClient;
    private readonly WorkerConfiguration _workerConfig;
    private readonly ILogger<BackupStatusReporter> _logger;

    public BackupStatusReporter(
        HttpClient httpClient,
        WorkerConfiguration workerConfig,
        ILogger<BackupStatusReporter> logger)
    {
        _httpClient = httpClient;
        _workerConfig = workerConfig;
        _logger = logger;
    }

    public async Task ReportStartedAsync(Guid jobId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reporting backup started for Job {JobId}", jobId);

        var request = new UpdateBackupStatusRequest
        {
            JobId = jobId,
            TenantId = tenantId,
            Status = BackupStatus.InProgress,
            ProgressPercentage = 0,
            CurrentStep = "Starting backup execution"
        };

        await SendStatusUpdateAsync(request, cancellationToken);
    }

    public async Task ReportProgressAsync(
        Guid jobId,
        Guid tenantId,
        int progressPercentage,
        string currentStep,
        long processedBytes,
        long totalBytes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reporting backup progress for Job {JobId}: {Progress}%", jobId, progressPercentage);

        var request = new UpdateBackupStatusRequest
        {
            JobId = jobId,
            TenantId = tenantId,
            Status = BackupStatus.InProgress,
            ProgressPercentage = progressPercentage,
            CurrentStep = currentStep,
            ProcessedBytes = processedBytes,
            TotalBytes = totalBytes
        };

        await SendStatusUpdateAsync(request, cancellationToken);
    }

    public async Task ReportCompletedAsync(
        Guid jobId,
        Guid tenantId,
        string blobUrl,
        string blobName,
        long backupSizeBytes,
        string compressionType,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reporting backup completed for Job {JobId}, Size: {SizeMB} MB",
            jobId, backupSizeBytes / (1024.0 * 1024.0));

        var request = new UpdateBackupStatusRequest
        {
            JobId = jobId,
            TenantId = tenantId,
            Status = BackupStatus.Completed,
            BlobUrl = blobUrl,
            BlobName = blobName,
            BackupSizeBytes = backupSizeBytes,
            CompressionType = compressionType,
            Metadata = metadata
        };

        await SendStatusUpdateAsync(request, cancellationToken);
    }

    public async Task ReportFailedAsync(
        Guid jobId,
        Guid tenantId,
        string errorMessage,
        string? errorCode = null,
        string? stackTrace = null,
        int retryCount = 0,
        bool willRetry = false,
        DateTime? nextRetryAt = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError("Reporting backup failed for Job {JobId}: {ErrorMessage}", jobId, errorMessage);

        var request = new UpdateBackupStatusRequest
        {
            JobId = jobId,
            TenantId = tenantId,
            Status = BackupStatus.Failed,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            StackTrace = stackTrace,
            RetryCount = retryCount,
            WillRetry = willRetry,
            NextRetryAt = nextRetryAt
        };

        await SendStatusUpdateAsync(request, cancellationToken);
    }

    private async Task SendStatusUpdateAsync(UpdateBackupStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/backups/update-status",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to update backup status for Job {JobId}. Status: {StatusCode}, Error: {Error}",
                    request.JobId, response.StatusCode, error);
            }
            else
            {
                _logger.LogDebug("Successfully updated backup status for Job {JobId} to {Status}",
                    request.JobId, request.Status);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while updating backup status for Job {JobId}", request.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating backup status for Job {JobId}", request.JobId);
        }
    }
}

/// <summary>
/// Request model matching API endpoint
/// </summary>
internal class UpdateBackupStatusRequest
{
    public Guid JobId { get; set; }
    public Guid TenantId { get; set; }
    public BackupStatus Status { get; set; }
    
    // For InProgress status
    public int? ProgressPercentage { get; set; }
    public string? CurrentStep { get; set; }
    public long? ProcessedBytes { get; set; }
    public long? TotalBytes { get; set; }
    
    // For Completed status
    public string? BlobUrl { get; set; }
    public string? BlobName { get; set; }
    public long? BackupSizeBytes { get; set; }
    public string? CompressionType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    
    // For Failed status
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public string? StackTrace { get; set; }
    public int? RetryCount { get; set; }
    public bool? WillRetry { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Backup status enum matching API
/// </summary>
internal enum BackupStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Timeout = 6
}
