using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MasterBackup_API.Application.Common.Interfaces;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage implementation for backup file management
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _containerPrefix;

    public AzureBlobStorageService(
        IConfiguration configuration, 
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("AzureStorage:ConnectionString is not configured");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerPrefix = configuration["AzureStorage:ContainerPrefix"] ?? "backups";
        
        _logger.LogInformation("AzureBlobStorageService initialized with container prefix: {Prefix}", _containerPrefix);
    }

    /// <summary>
    /// Get container name for tenant
    /// </summary>
    private string GetContainerName(Guid tenantId)
    {
        return $"{_containerPrefix}-{tenantId}".ToLowerInvariant();
    }

    /// <summary>
    /// Ensure container exists for tenant
    /// </summary>
    public async Task EnsureContainerExistsAsync(Guid tenantId)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            _logger.LogInformation("Container ensured for tenant {TenantId}: {ContainerName}", 
                tenantId, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring container exists for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Upload backup file to Azure Blob Storage
    /// </summary>
    public async Task<BlobUploadResult> UploadBackupAsync(
        Guid tenantId, 
        string fileName, 
        Stream stream, 
        Dictionary<string, string>? metadata = null,
        string contentType = "application/octet-stream")
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            
            // Get blob client
            var blobClient = containerClient.GetBlobClient(fileName);
            
            // Upload options
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Add upload timestamp to metadata
            uploadOptions.Metadata["uploaded_at"] = DateTime.UtcNow.ToString("o");
            uploadOptions.Metadata["tenant_id"] = tenantId.ToString();

            // Upload blob
            var response = await blobClient.UploadAsync(stream, uploadOptions);
            
            var result = new BlobUploadResult
            {
                BlobUrl = blobClient.Uri.ToString(),
                BlobName = fileName,
                SizeBytes = stream.Length,
                UploadedAt = DateTime.UtcNow,
                ContentType = contentType,
                ETag = response.Value.ETag.ToString()
            };

            _logger.LogInformation(
                "Uploaded backup file {FileName} ({SizeMB:F2} MB) for tenant {TenantId}",
                fileName, stream.Length / 1024.0 / 1024.0, tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading backup file {FileName} for tenant {TenantId}", 
                fileName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Download backup file from Azure Blob Storage
    /// </summary>
    public async Task<BackupDownloadResult> DownloadBackupAsync(Guid tenantId, string fileName)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Backup file {fileName} not found for tenant {tenantId}");
            }

            // Download blob
            var response = await blobClient.DownloadStreamingAsync();
            var properties = await blobClient.GetPropertiesAsync();

            var result = new BackupDownloadResult
            {
                Content = response.Value.Content,
                ContentType = properties.Value.ContentType,
                SizeBytes = properties.Value.ContentLength,
                Metadata = properties.Value.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            _logger.LogInformation(
                "Downloaded backup file {FileName} ({SizeMB:F2} MB) for tenant {TenantId}",
                fileName, properties.Value.ContentLength / 1024.0 / 1024.0, tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup file {FileName} for tenant {TenantId}", 
                fileName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Delete backup file from Azure Blob Storage
    /// </summary>
    public async Task<bool> DeleteBackupAsync(Guid tenantId, string fileName)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation(
                    "Deleted backup file {FileName} for tenant {TenantId}",
                    fileName, tenantId);
            }
            else
            {
                _logger.LogWarning(
                    "Backup file {FileName} not found for deletion (tenant {TenantId})",
                    fileName, tenantId);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup file {FileName} for tenant {TenantId}", 
                fileName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// List all backup files for a tenant
    /// </summary>
    public async Task<List<BlobMetadata>> ListBackupsAsync(Guid tenantId, string? prefix = null)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Check if container exists
            if (!await containerClient.ExistsAsync())
            {
                _logger.LogWarning("Container {ContainerName} does not exist for tenant {TenantId}", 
                    containerName, tenantId);
                return new List<BlobMetadata>();
            }

            var blobs = new List<BlobMetadata>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                
                var metadata = new BlobMetadata
                {
                    Name = blobItem.Name,
                    Url = blobClient.Uri.ToString(),
                    SizeBytes = blobItem.Properties.ContentLength ?? 0,
                    CreatedAt = blobItem.Properties.CreatedOn?.DateTime ?? DateTime.MinValue,
                    LastModified = blobItem.Properties.LastModified?.DateTime,
                    ContentType = blobItem.Properties.ContentType ?? "application/octet-stream",
                    Metadata = blobItem.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    ETag = blobItem.Properties.ETag?.ToString()
                };

                blobs.Add(metadata);
            }

            _logger.LogInformation(
                "Listed {Count} backup files for tenant {TenantId} (prefix: {Prefix})",
                blobs.Count, tenantId, prefix ?? "none");

            return blobs.OrderByDescending(b => b.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backup files for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Generate SAS URL for temporary access to backup file
    /// </summary>
    public async Task<string> GetBlobSasUrlAsync(Guid tenantId, string fileName, int expiryMinutes = 60)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Backup file {fileName} not found for tenant {tenantId}");
            }

            // Check if we can generate SAS tokens
            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException(
                    "Cannot generate SAS URL. Ensure the storage account is configured with account key.");
            }

            // Create SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                Resource = "b", // blob
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            // Set permissions (read only)
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogInformation(
                "Generated SAS URL for backup file {FileName} (tenant {TenantId}, expires in {Minutes} minutes)",
                fileName, tenantId, expiryMinutes);

            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SAS URL for file {FileName} (tenant {TenantId})", 
                fileName, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Get storage statistics for tenant container
    /// </summary>
    public async Task<ContainerStats> GetContainerStatsAsync(Guid tenantId)
    {
        try
        {
            var containerName = GetContainerName(tenantId);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync())
            {
                return new ContainerStats
                {
                    ContainerName = containerName,
                    BlobCount = 0,
                    TotalSizeBytes = 0
                };
            }

            int blobCount = 0;
            long totalSize = 0;

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                blobCount++;
                totalSize += blobItem.Properties.ContentLength ?? 0;
            }

            var stats = new ContainerStats
            {
                ContainerName = containerName,
                BlobCount = blobCount,
                TotalSizeBytes = totalSize
            };

            _logger.LogInformation(
                "Container stats for tenant {TenantId}: {Count} blobs, {SizeGB:F2} GB",
                tenantId, blobCount, stats.TotalSizeGB);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting container stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Check if blob storage is healthy
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            // Try to get account info to verify connection
            var accountInfo = await _blobServiceClient.GetAccountInfoAsync();
            return accountInfo != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob storage health check failed");
            return false;
        }
    }
}
