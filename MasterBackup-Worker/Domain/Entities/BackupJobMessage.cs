using MasterBackup_Worker.Domain.Enums;

namespace MasterBackup_Worker.Domain.Entities;

public class BackupJobMessage
{
    public Guid BackupExecutionId { get; set; }
    public Guid DatabaseConnectionId { get; set; }
    public Guid TenantId { get; set; }
    public DatabaseType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? SSLMode { get; set; }
    public bool IsInstant { get; set; }
    
    // Worker Assignment fields
    public string AssignmentMode { get; set; } = "Auto";
    public Guid? AssignedWorkerId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
