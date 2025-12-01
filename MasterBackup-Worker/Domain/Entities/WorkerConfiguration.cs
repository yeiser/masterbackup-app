namespace MasterBackup_Worker.Domain.Entities;

public class WorkerConfiguration
{
    public Guid WorkerId { get; set; }
    public Guid TenantId { get; set; }
    public string WorkerName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] SupportedDatabaseTypes { get; set; } = Array.Empty<string>();
    public int MaxConcurrentJobs { get; set; } = 1;
}
