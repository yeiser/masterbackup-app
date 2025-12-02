using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MasterBackup_Worker.Infrastructure.Http;

/// <summary>
/// HTTP client for communicating with the MasterBackup API
/// </summary>
public class ApiClient : IApiClient
{
    private readonly ILogger<ApiClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ApiClient(
        ILogger<ApiClient> logger,
        string apiBaseUrl,
        string apiKey)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        if (string.IsNullOrEmpty(apiBaseUrl))
            throw new ArgumentNullException(nameof(apiBaseUrl));

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    public async Task NotifyTestConnectionCompletedAsync(Guid connectionId, bool success, string message)
    {
        _logger.LogInformation("Notifying API of test connection result for {ConnectionId}: {Success}",
            connectionId, success);

        try
        {
            // Extraer versión del servidor del mensaje si existe
            string? serverVersion = ExtractServerVersion(message, success);

            var request = new
            {
                ConnectionId = connectionId,
                Success = success,
                Message = message,
                ServerVersion = serverVersion
            };

            var response = await _httpClient.PostAsJsonAsync("/api/test-connections/complete", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified API of test connection result for {ConnectionId}", connectionId);
            }
            else
            {
                _logger.LogWarning("Failed to notify API of test connection result for {ConnectionId}. Status: {StatusCode}, Reason: {Reason}",
                    connectionId, response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error notifying API of test connection result for {ConnectionId}", connectionId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout notifying API of test connection result for {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error notifying API of test connection result for {ConnectionId}", connectionId);
        }
    }

    public async Task UpdateBackupExecutionStatusAsync(Guid backupExecutionId, string status, string? errorMessage = null)
    {
        _logger.LogInformation("Updating backup execution {BackupExecutionId} status to {Status}",
            backupExecutionId, status);

        try
        {
            var request = new
            {
                BackupExecutionId = backupExecutionId,
                Status = status,
                ErrorMessage = errorMessage
            };

            var response = await _httpClient.PostAsJsonAsync("/api/backup-executions/update-status", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated backup execution status for {BackupExecutionId}", backupExecutionId);
            }
            else
            {
                _logger.LogWarning("Failed to update backup execution status for {BackupExecutionId}. Status: {StatusCode}",
                    backupExecutionId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating backup execution status for {BackupExecutionId}", backupExecutionId);
        }
    }

    public async Task UploadBackupFileMetadataAsync(Guid backupExecutionId, string fileName, string blobUrl, long fileSize)
    {
        _logger.LogInformation("Uploading backup file metadata for {BackupExecutionId}: {FileName} ({FileSize} bytes)",
            backupExecutionId, fileName, fileSize);

        try
        {
            var request = new
            {
                BackupExecutionId = backupExecutionId,
                FileName = fileName,
                BlobUrl = blobUrl,
                FileSize = fileSize
            };

            var response = await _httpClient.PostAsJsonAsync("/api/backup-executions/upload-metadata", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully uploaded backup file metadata for {BackupExecutionId}", backupExecutionId);
            }
            else
            {
                _logger.LogWarning("Failed to upload backup file metadata for {BackupExecutionId}. Status: {StatusCode}",
                    backupExecutionId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading backup file metadata for {BackupExecutionId}", backupExecutionId);
        }
    }

    public async Task<(bool Success, Guid WorkerId, Guid TenantId, string Message)> RegisterWorkerAsync(
        string name,
        string? hostname,
        string? osInfo,
        string[] supportedDatabaseTypes,
        int maxConcurrentJobs,
        string? version,
        string[] tags)
    {
        _logger.LogInformation("Registering worker {WorkerName} with API at {BaseUrl}", name, _httpClient.BaseAddress);
        _logger.LogInformation("API Key: {ApiKey}", _apiKey.Substring(0, Math.Min(8, _apiKey.Length)) + "...");

        try
        {
            var request = new
            {
                Name = name,
                Hostname = hostname,
                OsInfo = osInfo,
                SupportedDatabaseTypes = supportedDatabaseTypes,
                MaxConcurrentJobs = maxConcurrentJobs,
                Version = version,
                Tags = tags
            };

            _logger.LogInformation("Sending registration request...");
            var response = await _httpClient.PostAsJsonAsync("/api/workers/register", request);
            _logger.LogInformation("Response received with status code: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterWorkerResponse>();
                
                if (result != null)
                {
                    _logger.LogInformation("Successfully registered worker. WorkerId: {WorkerId}, TenantId: {TenantId}",
                        result.WorkerId, result.TenantId);
                    return (true, result.WorkerId, result.TenantId, result.Message);
                }
                
                return (false, Guid.Empty, Guid.Empty, "Invalid response from API");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to register worker. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return (false, Guid.Empty, Guid.Empty, $"Registration failed: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error registering worker");
            return (false, Guid.Empty, Guid.Empty, $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout registering worker");
            return (false, Guid.Empty, Guid.Empty, "Registration timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error registering worker");
            return (false, Guid.Empty, Guid.Empty, $"Unexpected error: {ex.Message}");
        }
    }

    public async Task SendHeartbeatAsync(
        Guid workerId,
        string status,
        int currentActiveJobs,
        long? totalBackupsProcessed = null,
        long? totalBytesProcessed = null)
    {
        _logger.LogDebug("Sending heartbeat for worker {WorkerId}", workerId);

        try
        {
            var request = new
            {
                WorkerId = workerId,
                Status = status,
                CurrentActiveJobs = currentActiveJobs,
                TotalBackupsProcessed = totalBackupsProcessed,
                TotalBytesProcessed = totalBytesProcessed
            };

            var response = await _httpClient.PostAsJsonAsync("/api/workers/heartbeat", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Heartbeat sent successfully for worker {WorkerId}", workerId);
            }
            else
            {
                _logger.LogWarning("Failed to send heartbeat for worker {WorkerId}. Status: {StatusCode}",
                    workerId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat for worker {WorkerId}", workerId);
        }
    }

    /// <summary>
    /// Extracts database server version from test connection message
    /// </summary>
    private string? ExtractServerVersion(string message, bool success)
    {
        if (!success)
            return null;

        // PostgreSQL: "Connection successful! PostgreSQL version: PostgreSQL 15.3..."
        var postgresMatch = Regex.Match(message, @"PostgreSQL version:\s*PostgreSQL\s+([\d.]+)", RegexOptions.IgnoreCase);
        if (postgresMatch.Success)
            return postgresMatch.Groups[1].Value;

        // MySQL: "Connection successful! MySQL version: 8.0.33..."
        var mysqlMatch = Regex.Match(message, @"MySQL version:\s*([\d.]+)", RegexOptions.IgnoreCase);
        if (mysqlMatch.Success)
            return mysqlMatch.Groups[1].Value;

        // SQL Server: "Connection successful! SQL Server version: 15.00.2000..."
        var sqlServerMatch = Regex.Match(message, @"SQL Server version:\s*([\d.]+)", RegexOptions.IgnoreCase);
        if (sqlServerMatch.Success)
            return sqlServerMatch.Groups[1].Value;

        return null;
    }
    
    // Response models
    private class RegisterWorkerResponse
    {
        public Guid WorkerId { get; set; }
        public Guid TenantId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
