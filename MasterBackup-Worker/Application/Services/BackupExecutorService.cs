using System.Diagnostics;
using System.IO.Compression;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;
using Npgsql;

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

            // Step 5: Auto-restore if configured
            if (message.AutoRestore && !string.IsNullOrEmpty(message.TargetDatabaseName))
            {
                _logger.LogInformation("Auto-restore enabled for Job {JobId}. Target database: {TargetDatabase}",
                    message.JobId, message.TargetDatabaseName);

                try
                {
                    await ExecuteAutoRestoreAsync(message, localFilePath);
                    _logger.LogInformation("Auto-restore completed successfully for Job {JobId}", message.JobId);
                }
                catch (Exception restoreEx)
                {
                    _logger.LogError(restoreEx, "Auto-restore failed for Job {JobId}: {Error}", 
                        message.JobId, restoreEx.Message);
                    // Note: We don't fail the backup if restore fails, just log the error
                }
            }

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

    /// <summary>
    /// Executes auto-restore of the backup to a new database with _DW suffix
    /// </summary>
    private async Task ExecuteAutoRestoreAsync(BackupJobMessage message, string backupFilePath)
    {
        _logger.LogInformation("Starting auto-restore for Job {JobId} to database {TargetDatabase}",
            message.JobId, message.TargetDatabaseName);

        var connectionParams = ParseConnectionString(message.DatabaseConnection.ConnectionString);
        
        if (!connectionParams.TryGetValue("Host", out var host) ||
            !connectionParams.TryGetValue("Username", out var username) ||
            !connectionParams.TryGetValue("Password", out var password))
        {
            throw new InvalidOperationException("Invalid connection string: missing Host, Username or Password");
        }

        var port = connectionParams.TryGetValue("Port", out var portStr) ? portStr : "5432";

        // Step 1: Drop target database if exists and create new one
        _logger.LogInformation("Creating target database {TargetDatabase}", message.TargetDatabaseName);
        await CreateTargetDatabaseAsync(host, port, username, password, message.TargetDatabaseName!);

        // Step 2: Execute pg_restore on the target database
        _logger.LogInformation("Restoring backup to database {TargetDatabase}", message.TargetDatabaseName);
        await ExecutePgRestoreAsync(host, port, username, password, message.TargetDatabaseName!, backupFilePath);

        _logger.LogInformation("Auto-restore completed for database {TargetDatabase}", message.TargetDatabaseName);
    }

    /// <summary>
    /// Creates a new target database, dropping it first if it exists
    /// </summary>
    private async Task CreateTargetDatabaseAsync(string host, string port, string username, string password, string databaseName)
    {
        // Connect to postgres database to create target database
        var postgresConnString = $"Host={host};Port={port};Database=postgres;Username={username};Password={password}";
        
        await using var connection = new Npgsql.NpgsqlConnection(postgresConnString);
        await connection.OpenAsync();

        // Terminate existing connections to target database
        var terminateQuery = $@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{databaseName}'
              AND pid <> pg_backend_pid();";

        await using (var terminateCmd = new Npgsql.NpgsqlCommand(terminateQuery, connection))
        {
            await terminateCmd.ExecuteNonQueryAsync();
        }

        // Drop database if exists
        var dropQuery = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
        await using (var dropCmd = new Npgsql.NpgsqlCommand(dropQuery, connection))
        {
            await dropCmd.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Dropped existing database {Database} (if existed)", databaseName);

        // Create new database
        var createQuery = $"CREATE DATABASE \"{databaseName}\";";
        await using (var createCmd = new Npgsql.NpgsqlCommand(createQuery, connection))
        {
            await createCmd.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Created new database {Database}", databaseName);
    }

    /// <summary>
    /// Executes pg_restore to restore the backup file to the target database
    /// </summary>
    private async Task ExecutePgRestoreAsync(string host, string port, string username, string password, string databaseName, string backupFilePath)
    {
        var pgRestorePath = _configuration["PostgreSQL:PgRestorePath"] ?? "pg_restore";

        var startInfo = new ProcessStartInfo
        {
            FileName = pgRestorePath,
            Arguments = $"--host={host} --port={port} --username={username} --dbname=\"{databaseName}\" --verbose --clean --if-exists --no-owner --no-privileges \"{backupFilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set password via environment variable
        startInfo.Environment["PGPASSWORD"] = password;

        _logger.LogDebug("Starting pg_restore: {FileName} {Arguments}", pgRestorePath, 
            startInfo.Arguments.Replace(password, "***"));

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start pg_restore process");

        // Read output and errors
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(output))
            _logger.LogDebug("pg_restore output: {Output}", output);

        // pg_restore may return non-zero exit codes for warnings, so we check stderr instead
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            // Check if errors are just warnings (common with pg_restore)
            if (error.Contains("ERROR") && !error.Contains("already exists"))
            {
                _logger.LogWarning("pg_restore completed with warnings: {Error}", error);
            }
            else
            {
                _logger.LogDebug("pg_restore warnings (non-critical): {Error}", error);
            }
        }

        _logger.LogInformation("pg_restore completed with exit code {ExitCode}", process.ExitCode);
    }
}
