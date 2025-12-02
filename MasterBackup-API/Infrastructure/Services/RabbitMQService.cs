using MasterBackup_API.Application.Common.Interfaces;
using MasterBackup_API.Domain.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MasterBackup_API.Infrastructure.Services;

/// <summary>
/// RabbitMQ implementation for message queue operations
/// </summary>
public class RabbitMQService : IMessageQueueService, IDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly string _exchangeName = "masterbackup.exchange";
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

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
            VirtualHost = rabbitMQConfig["VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };
    }

    /// <summary>
    /// Ensure connection and channel are open
    /// </summary>
    private async Task EnsureConnectionAsync()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = await _factory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established");
        }

        if (_channel == null || !_channel.IsOpen)
        {
            _channel = await _connection.CreateChannelAsync();
            
            // Declare main exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );
            
            _logger.LogInformation("RabbitMQ channel created and exchange declared");
        }
    }

    /// <summary>
    /// Publish a backup job message to the tenant's backup queue
    /// </summary>
    public async Task PublishBackupJobAsync(Guid tenantId, BackupJobMessage message)
    {
        try
        {
            await EnsureConnectionAsync();

            var routingKey = $"backup.job.{tenantId}";
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.JobId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    { "tenant-id", tenantId.ToString() },
                    { "backup-schedule-id", message.BackupScheduleId.ToString() },
                    { "retry-count", message.CurrentRetry }
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Backup job {JobId} published for tenant {TenantId}, schedule {ScheduleId}",
                message.JobId, tenantId, message.BackupScheduleId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing backup job {JobId} for tenant {TenantId}", 
                message.JobId, tenantId);
            throw;
        }
    }

    /// <summary>
    /// Publish a test connection message to the tenant's test-connection queue
    /// </summary>
    public async Task PublishTestConnectionAsync(Guid tenantId, object message)
    {
        try
        {
            await EnsureConnectionAsync();

            var queueName = $"{tenantId}.test-connection.queue";
            var routingKey = $"test-connection.{tenantId}";
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Declare the test-connection queue if it doesn't exist
            await _channel!.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Bind queue to exchange
            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: routingKey
            );

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    { "tenant-id", tenantId.ToString() },
                    { "message-type", "TestConnection" }
                }
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Test connection message published for tenant {TenantId} to queue {QueueName}",
                tenantId, queueName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing test connection message for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Create exchange and queues for a tenant
    /// </summary>
    public async Task CreateTenantQueueAsync(Guid tenantId)
    {
        try
        {
            await EnsureConnectionAsync();

            // Queue names
            var backupQueueName = $"backup.jobs.{tenantId}";
            var resultQueueName = $"backup.results.{tenantId}";
            var dlqName = $"backup.jobs.{tenantId}.dlq";

            // Declare Dead Letter Queue
            await _channel!.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Declare backup jobs queue with DLQ
            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _exchangeName },
                { "x-dead-letter-routing-key", $"backup.dlq.{tenantId}" },
                { "x-message-ttl", 3600000 } // 1 hour TTL
            };

            await _channel.QueueDeclareAsync(
                queue: backupQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs
            );

            // Bind backup queue to exchange
            await _channel.QueueBindAsync(
                queue: backupQueueName,
                exchange: _exchangeName,
                routingKey: $"backup.job.{tenantId}"
            );

            // Bind DLQ to exchange
            await _channel.QueueBindAsync(
                queue: dlqName,
                exchange: _exchangeName,
                routingKey: $"backup.dlq.{tenantId}"
            );

            // Declare results queue
            await _channel.QueueDeclareAsync(
                queue: resultQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Bind results queue
            await _channel.QueueBindAsync(
                queue: resultQueueName,
                exchange: _exchangeName,
                routingKey: $"backup.result.{tenantId}"
            );

            _logger.LogInformation(
                "Created queues for tenant {TenantId}: {BackupQueue}, {ResultQueue}, {DLQ}",
                tenantId, backupQueueName, resultQueueName, dlqName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating queues for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Delete tenant queues and unbind from exchange
    /// </summary>
    public async Task DeleteTenantQueueAsync(Guid tenantId)
    {
        try
        {
            await EnsureConnectionAsync();

            var backupQueueName = $"backup.jobs.{tenantId}";
            var resultQueueName = $"backup.results.{tenantId}";
            var dlqName = $"backup.jobs.{tenantId}.dlq";

            // Delete queues (this also unbinds them)
            await _channel!.QueueDeleteAsync(queue: backupQueueName, ifUnused: false, ifEmpty: false);
            await _channel.QueueDeleteAsync(queue: resultQueueName, ifUnused: false, ifEmpty: false);
            await _channel.QueueDeleteAsync(queue: dlqName, ifUnused: false, ifEmpty: false);

            _logger.LogInformation("Deleted queues for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting queues for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Subscribe to backup result messages from workers
    /// </summary>
    public async Task SubscribeToBackupResultsAsync(Guid tenantId, Func<BackupJobResult, Task> onMessageReceived)
    {
        try
        {
            await EnsureConnectionAsync();

            var resultQueueName = $"backup.results.{tenantId}";

            var consumer = new AsyncEventingBasicConsumer(_channel!);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var result = JsonSerializer.Deserialize<BackupJobResult>(json);

                    if (result != null)
                    {
                        await onMessageReceived(result);
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        
                        _logger.LogInformation(
                            "Processed backup result {JobId} for tenant {TenantId}, Success: {Success}",
                            result.JobId, tenantId, result.Success
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing backup result message");
                    // Reject and requeue the message
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: resultQueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("Subscribed to backup results for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to backup results for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Check if RabbitMQ connection is healthy
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            await EnsureConnectionAsync();
            return _connection?.IsOpen == true && _channel?.IsOpen == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return false;
        }
    }

    /// <summary>
    /// Get queue statistics
    /// </summary>
    public async Task<QueueStats> GetQueueStatsAsync(Guid tenantId)
    {
        try
        {
            await EnsureConnectionAsync();

            var queueName = $"backup.jobs.{tenantId}";
            var queueDeclareOk = await _channel!.QueueDeclarePassiveAsync(queue: queueName);

            return new QueueStats
            {
                QueueName = queueName,
                MessageCount = queueDeclareOk.MessageCount,
                ConsumerCount = queueDeclareOk.ConsumerCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue stats for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _channel?.Dispose();
        _connection?.Dispose();
        _disposed = true;
        
        _logger.LogInformation("RabbitMQ connection disposed");
    }
}
