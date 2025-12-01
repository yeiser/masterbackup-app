using MasterBackup_Worker.Domain.Entities;

namespace MasterBackup_Worker.Application.Interfaces;

/// <summary>
/// Interface for executing database backups
/// </summary>
public interface IBackupExecutorService
{
    /// <summary>
    /// Executes a database backup
    /// </summary>
    /// <param name="message">Backup job details</param>
    /// <returns>Result of the backup operation</returns>
    Task<(bool Success, string FilePath, long FileSize, string Message)> ExecuteBackupAsync(BackupJobMessage message);
}
