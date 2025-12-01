using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Common.DTOs;

public class CreateDatabaseConnectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DatabaseType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? EngineVersion { get; set; }
    public string? SSLMode { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Worker Assignment
    public Guid? AssignedWorkerId { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public WorkerAssignmentMode AssignmentMode { get; set; } = WorkerAssignmentMode.Auto;
}
