namespace MasterBackup_API.Application.Common.Interfaces;

/// <summary>
/// Service for Azure Blob Storage operations
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a backup file to Azure Blob Storage
    /// </summary>
    /// <param name="tenantId">Tenant identifier for container isolation</param>
    /// <param name="fileName">Name of the blob file</param>
    /// <param name="stream">File stream to upload</param>
    /// <param name="metadata">Optional metadata to attach to blob</param>
    /// <param name="contentType">Content type (default: application/octet-stream)</param>
    /// <returns>Blob URL and metadata</returns>
    Task<BlobUploadResult> UploadBackupAsync(
        Guid tenantId, 
        string fileName, 
        Stream stream, 
        Dictionary<string, string>? metadata = null,
        string contentType = "application/octet-stream");

    /// <summary>
    /// Download a backup file from Azure Blob Storage
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="fileName">Name of the blob file</param>
    /// <returns>File stream and metadata</returns>
    Task<BackupDownloadResult> DownloadBackupAsync(Guid tenantId, string fileName);

    /// <summary>
    /// Delete a backup file from Azure Blob Storage
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="fileName">Name of the blob file</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteBackupAsync(Guid tenantId, string fileName);

    /// <summary>
    /// List all backup files for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="prefix">Optional prefix to filter files</param>
    /// <returns>List of blob metadata</returns>
    Task<List<BlobMetadata>> ListBackupsAsync(Guid tenantId, string? prefix = null);

    /// <summary>
    /// Generate a SAS URL for temporary access to a backup file
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="fileName">Name of the blob file</param>
    /// <param name="expiryMinutes">Expiry time in minutes (default: 60)</param>
    /// <returns>SAS URL with expiry time</returns>
    Task<string> GetBlobSasUrlAsync(Guid tenantId, string fileName, int expiryMinutes = 60);

    /// <summary>
    /// Get storage statistics for a tenant container
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Container statistics</returns>
    Task<ContainerStats> GetContainerStatsAsync(Guid tenantId);

    /// <summary>
    /// Check if blob storage is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// Create container for tenant if it doesn't exist
    /// </summary>
    Task EnsureContainerExistsAsync(Guid tenantId);
}

/// <summary>
/// Result of blob upload operation
/// </summary>
public class BlobUploadResult
{
    public string BlobUrl { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ETag { get; set; }
}

/// <summary>
/// Result of blob download operation
/// </summary>
public class BackupDownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Metadata for a blob file
/// </summary>
public class BlobMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string? ETag { get; set; }
}

/// <summary>
/// Storage statistics for a container
/// </summary>
public class ContainerStats
{
    public string ContainerName { get; set; } = string.Empty;
    public int BlobCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMB => TotalSizeBytes / 1024.0 / 1024.0;
    public double TotalSizeGB => TotalSizeBytes / 1024.0 / 1024.0 / 1024.0;
}
