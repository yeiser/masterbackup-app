using MasterBackup_API.Application.Common.DTOs;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MasterBackup_API.Infrastructure.Services;

public class RabbitMQService : Application.Common.Interfaces.IMessageQueueService
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        
        var rabbitMQConfig = configuration.GetSection("RabbitMQ");
        _factory = new ConnectionFactory
        {
            HostName = rabbitMQConfig["HostName"] ?? "localhost",
            Port = int.Parse(rabbitMQConfig["Port"] ?? "5672"),
            UserName = rabbitMQConfig["UserName"] ?? "guest",
            Password = rabbitMQConfig["Password"] ?? "guest",
            VirtualHost = rabbitMQConfig["VirtualHost"] ?? "/"
        };
    }

    public async Task PublishTestConnectionJob(Guid tenantId, TestConnectionMessage message)
    {
        await Task.Run(async () =>
        {
            var queueName = $"{tenantId}.test-connection.queue";

            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Declarar queue si no existe
            await channel.QueueDeclareAsync(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(exchange: "",
                                 routingKey: queueName,
                                 mandatory: false,
                                 basicProperties: properties,
                                 body: body);

            _logger.LogInformation("Test connection job published to queue {QueueName} for connection {ConnectionId}", 
                queueName, message.ConnectionId);
        });
    }

    public void CreateTenantQueue(Guid tenantId)
    {
        Task.Run(async () =>
        {
            var queueName = $"{tenantId}.test-connection.queue";

            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            _logger.LogInformation("Created queue {QueueName} for tenant {TenantId}", queueName, tenantId);
        }).Wait();
    }

    public void DeleteTenantQueue(Guid tenantId)
    {
        Task.Run(async () =>
        {
            var queueName = $"{tenantId}.test-connection.queue";

            await using var connection = await _factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeleteAsync(queue: queueName);

            _logger.LogInformation("Deleted queue {QueueName} for tenant {TenantId}", queueName, tenantId);
        }).Wait();
    }
}
