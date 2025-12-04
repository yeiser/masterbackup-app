using System.Diagnostics;
using System.IO.Compression;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Application.Services;

/// <summary>
/// Service for executing database backups with status reporting
/// </summary>
public class BackupExecutorService : IBackupExecutorService
{
    private readonly IBackupStatusReporter _statusReporter;
    private readonly ILogger<BackupExecutorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _tempPath;

    public BackupExecutorService(
        IBackupStatusReporter statusReporter,
        ILogger<BackupExecutorService> logger,
        IConfiguration configuration)
    {
        _statusReporter = statusReporter ?? throw new ArgumentNullException(nameof(statusReporter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(logger));
        _tempPath = Path.Combine(Path.GetTempPath(), "masterbackup-worker");
        Directory.CreateDirectory(_tempPath);
    }

    public async Task<(bool Success, string FilePath, long FileSize, string Message)> ExecuteBackupAsync(BackupJobMessage message)
    {
        var startTime = DateTime.UtcNow;
        string? localFilePath = null;
        string? compressedFilePath = null;

        try
        {
            _logger.LogInformation("Starting backup execution for Job {JobId}, Database: {DatabaseName}",
                message.JobId, message.DatabaseConnection.Name);

            // Report backup started
            await _statusReporter.ReportStartedAsync(message.JobId, message.TenantId);

            // Step 1: Execute database dump (25%)
            await _statusReporter.ReportProgressAsync(
                message.JobId, message.TenantId, 25, "Executing database dump", 0, 0);

            localFilePath = await ExecuteDatabaseDumpAsync(message);
            var fileInfo = new FileInfo(localFilePath);

            _logger.LogInformation("Database dump completed. Size: {SizeMB} MB", 
                fileInfo.Length / (1024.0 * 1024.0));

            // Step 2: Compress backup file (50%)
            await _statusReporter.ReportProgressAsync(
                message.JobId, message.TenantId, 50, "Compressing backup file", 
                fileInfo.Length, fileInfo.Length);

            compressedFilePath = await CompressBackupAsync(localFilePath, message.CompressionType ?? "GZIP");
            var compressedInfo = new FileInfo(compressedFilePath);

            _logger.LogInformation("Backup compressed. Original: {OriginalMB} MB, Compressed: {CompressedMB} MB",
                fileInfo.Length / (1024.0 * 1024.0),
                compressedInfo.Length / (1024.0 * 1024.0));

            // Step 3: Upload to Azure Blob Storage (75%)
            await _statusReporter.ReportProgressAsync(
                message.JobId, message.TenantId, 75, "Uploading to blob storage",
                compressedInfo.Length, compressedInfo.Length);

            var blobUrl = await UploadToBlobStorageAsync(
                compressedFilePath,
                message.BlobStorageConnectionString,
                message.ContainerName,
                message.BackupFileName);

            _logger.LogInformation("Backup uploaded to blob storage: {BlobUrl}", blobUrl);

            // Step 4: Report completion (100%)
            var duration = DateTime.UtcNow - startTime;
            var metadata = new Dictionary<string, string>
            {
                { "database_name", message.DatabaseConnection.Name },
                { "database_type", message.DatabaseConnection.DatabaseType },
                { "duration_seconds", duration.TotalSeconds.ToString("F2") },
                { "original_size_bytes", fileInfo.Length.ToString() },
                { "compressed_size_bytes", compressedInfo.Length.ToString() },
                { "compression_ratio", (compressedInfo.Length / (double)fileInfo.Length * 100).ToString("F2") + "%" }
            };

            await _statusReporter.ReportCompletedAsync(
                message.JobId,
                message.TenantId,
                blobUrl,
                message.BackupFileName,
                compressedInfo.Length,
                message.CompressionType ?? "GZIP",
                metadata);

            _logger.LogInformation("Backup Job {JobId} completed successfully in {Duration}",
                message.JobId, duration);

            return (true, compressedFilePath, compressedInfo.Length, "Backup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup Job {JobId} failed: {Error}", message.JobId, ex.Message);

            // Report failure
            await _statusReporter.ReportFailedAsync(
                message.JobId,
                message.TenantId,
                ex.Message,
                ex.GetType().Name,
                ex.StackTrace,
                message.CurrentRetry,
                message.CurrentRetry < message.MaxRetries,
                message.CurrentRetry < message.MaxRetries ? DateTime.UtcNow.AddMinutes(5) : null);

            return (false, string.Empty, 0, ex.Message);
        }
        finally
        {
            // Cleanup temporary files
            CleanupTempFiles(localFilePath, compressedFilePath);
        }
    }

    private async Task<string> ExecuteDatabaseDumpAsync(BackupJobMessage message)
    {
        var outputFilePath = Path.Combine(_tempPath, $"{Guid.NewGuid()}.sql");
        var databaseType = message.DatabaseConnection.DatabaseType.ToLower();

        _logger.LogInformation("Executing {DatabaseType} dump to {OutputFile}", databaseType, outputFilePath);

        try
        {
            switch (databaseType)
            {
                case "postgresql":
                    await ExecutePostgreSQLDumpAsync(message.DatabaseConnection.ConnectionString, outputFilePath);
                    break;

                case "mysql":
                    await ExecuteMySQLDumpAsync(message.DatabaseConnection.ConnectionString, outputFilePath);
                    break;

                case "sqlserver":
                    await ExecuteSQLServerDumpAsync(message.DatabaseConnection.ConnectionString, outputFilePath);
                    break;

                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported");
            }

            if (!File.Exists(outputFilePath) || new FileInfo(outputFilePath).Length == 0)
            {
                throw new InvalidOperationException("Backup file was not created or is empty");
            }

            return outputFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute database dump");
            throw;
        }
    }

    private async Task ExecutePostgreSQLDumpAsync(string connectionString, string outputFile)
    {
        // Parse connection string
        var connDict = ParseConnectionString(connectionString);
        var host = connDict.GetValueOrDefault("Host", "localhost");
        var port = connDict.GetValueOrDefault("Port", "5432");
        var database = connDict.GetValueOrDefault("Database", "");
        var username = connDict.GetValueOrDefault("Username", "");
        var password = connDict.GetValueOrDefault("Password", "");

        // Get pg_dump path from configuration
        var pgDumpPath = _configuration["DatabaseTools:PostgreSQL:PgDumpPath"] ?? "pg_dump";
        
        _logger.LogDebug("Using pg_dump from: {PgDumpPath}", pgDumpPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = $"-h {host} -p {port} -U {username} -d {database} -F c -f \"{outputFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment = { ["PGPASSWORD"] = password }
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start pg_dump process");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"pg_dump failed with exit code {process.ExitCode}: {error}");
        }
    }

    private Task ExecuteMySQLDumpAsync(string connectionString, string outputFile)
    {
        // TODO: Implement MySQL dump using mysqldump
        throw new NotImplementedException("MySQL backup not yet implemented");
    }

    private Task ExecuteSQLServerDumpAsync(string connectionString, string outputFile)
    {
        // TODO: Implement SQL Server backup using sqlcmd or SMO
        throw new NotImplementedException("SQL Server backup not yet implemented");
    }

    private async Task<string> CompressBackupAsync(string inputFile, string compressionType)
    {
        var outputFile = inputFile + ".gz";

        _logger.LogInformation("Compressing backup file: {InputFile} -> {OutputFile}", inputFile, outputFile);

        using (var inputStream = File.OpenRead(inputFile))
        using (var outputStream = File.Create(outputFile))
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            await inputStream.CopyToAsync(gzipStream);
        }

        return outputFile;
    }

    private async Task<string> UploadToBlobStorageAsync(
        string filePath,
        string connectionString,
        string containerName,
        string blobName)
    {
        _logger.LogInformation("Uploading backup to container {Container}, blob {Blob}", containerName, blobName);

        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Ensure container exists
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);

        using (var fileStream = File.OpenRead(filePath))
        {
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }

        return blobClient.Uri.ToString();
    }

    private Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        return connectionString
            .Split(';')
            .Where(part => part.Contains('='))
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => parts[0].Trim(),
                parts => parts[1].Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private void CleanupTempFiles(params string?[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogDebug("Deleted temporary file: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {FilePath}", filePath);
                }
            }
        }
    }
}
