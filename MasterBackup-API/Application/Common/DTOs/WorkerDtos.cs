namespace MasterBackup_API.Application.Common.DTOs;

public class RegisterWorkerDto
{
    public Guid? WorkerId { get; set; } // Optional: for re-registration with same ID
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Hostname { get; set; }
    public string? OsInfo { get; set; }
    public string[] SupportedDatabaseTypes { get; set; } = Array.Empty<string>();
    public int MaxConcurrentJobs { get; set; } = 1;
    public string? Version { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class RegisterWorkerResponseDto
{
    public Guid WorkerId { get; set; }
    public Guid TenantId { get; set; }
    public RabbitMQSettingsDto RabbitMQSettings { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}

public class RabbitMQSettingsDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class WorkerHeartbeatDto
{
    public Guid WorkerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentActiveJobs { get; set; }
    public long? TotalBackupsProcessed { get; set; }
    public long? TotalBytesProcessed { get; set; }
}

public class WorkerDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? Hostname { get; set; }
    public string? OsInfo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public DateTime? LastJobExecution { get; set; }
    public string[] SupportedDatabaseTypes { get; set; } = Array.Empty<string>();
    public int MaxConcurrentJobs { get; set; }
    public int CurrentActiveJobs { get; set; }
    public string? Version { get; set; }
    public long TotalBackupsProcessed { get; set; }
    public long TotalBytesProcessed { get; set; }
    public bool IsActive { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WorkerStatsDto
{
    public int TotalWorkers { get; set; }
    public int OnlineWorkers { get; set; }
    public int OfflineWorkers { get; set; }
    public int BusyWorkers { get; set; }
    public long TotalBackupsProcessed { get; set; }
    public long TotalBytesProcessed { get; set; }
}
