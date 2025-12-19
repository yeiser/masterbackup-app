using MediatR;

namespace MasterBackup_API.Application.Features.Backups.Commands;

/// <summary>
/// Command to execute an instant (manual) backup
/// </summary>
public class ExecuteInstantBackupCommand : IRequest<ExecuteInstantBackupResult>
{
    public Guid DatabaseConnectionId { get; set; }
    public string? CompressionType { get; set; } = "GZIP";
    public int? TimeoutMinutes { get; set; }
    public int? MaxRetries { get; set; }
    public string? InitiatedBy { get; set; }
}

/// <summary>
/// Result of instant backup execution
/// </summary>
public class ExecuteInstantBackupResult
{
    public Guid BackupHistoryId { get; set; }
    public Guid JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
}
