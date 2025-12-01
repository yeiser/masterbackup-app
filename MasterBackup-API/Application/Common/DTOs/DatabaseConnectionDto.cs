namespace MasterBackup_API.Application.Common.DTOs;

public class DatabaseConnectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? EngineVersion { get; set; }
    public string? SSLMode { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSuccessful { get; set; }
    public string? LastTestStatus { get; set; }
    public int BackupSchedulesCount { get; set; }
    
    // Worker Assignment
    public Guid? AssignedWorkerId { get; set; }
    public string? AssignedWorkerName { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string AssignmentMode { get; set; } = "Auto";
}
