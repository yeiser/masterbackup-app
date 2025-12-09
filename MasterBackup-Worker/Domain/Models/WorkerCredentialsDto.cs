namespace MasterBackup_Worker.Domain.Models;

public class WorkerCredentialsDto
{
    public RabbitMQCredentials RabbitMQ { get; set; } = new();
    public string AzureStorageConnectionString { get; set; } = string.Empty;
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
