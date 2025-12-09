namespace MasterBackup_API.Application.Common.DTOs;

/// <summary>
/// Contains sensitive credentials that the worker needs to operate
/// These are provided by the API and never stored by the client
/// </summary>
public class WorkerCredentialsDto
{
    /// <summary>
    /// RabbitMQ connection settings
    /// </summary>
    public RabbitMQCredentials RabbitMQ { get; set; } = new();
    
    /// <summary>
    /// Azure Storage connection string
    /// </summary>
    public string AzureStorageConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Container prefix for this tenant's backups
    /// </summary>
    public string ContainerPrefix { get; set; } = "backups";
}

public class RabbitMQCredentials
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = string.Empty;
}
