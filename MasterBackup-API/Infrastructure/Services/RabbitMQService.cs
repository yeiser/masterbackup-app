using MasterBackup_API.Application.Common.DTOs;
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
    private readonly string _exchangeName = "masterbackup.jobs"; // Unified exchange
    private readonly string _exchangeType = "topic"; // Topic exchange for routing keys
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
            
            // Declare unified topic exchange for all job types
            await _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: _exchangeType,
                durable: true,
                autoDelete: false
            );
            
            _logger.LogInformation("RabbitMQ channel created and topic exchange '{ExchangeName}' declared", _exchangeName);
        }
    }

    /// <summary>
    /// Publish a backup job message to the tenant's unified queue with routing key
    /// </summary>
    public async Task PublishBackupJobAsync(Guid tenantId, BackupJobMessage message)
    {
        try
        {
            await EnsureConnectionAsync();

            // Routing key pattern: backup.execute.{tenantId}
            var routingKey = $"backup.execute.{tenantId}";
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
                    { "message-type", "BackupJob" },
                    { "tenant-id", tenantId.ToString() },
                    { "backup-schedule-id", message.BackupScheduleId.ToString() },
                    { "retry-count", message.CurrentRetry },
                    { "published-at", DateTime.UtcNow.ToString("O") }
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
    /// Publish a test connection message to the tenant's unified queue with routing key
    /// </summary>
    public async Task PublishTestConnectionAsync(Guid tenantId, TestConnectionMessage message)
    {
        try
        {
            await EnsureConnectionAsync();

            // Routing key pattern: backup.test.{tenantId}
            var routingKey = $"backup.test.{tenantId}";
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>
                {
                    { "message-type", "TestConnection" },
                    { "tenant-id", tenantId.ToString() },
                    { "connection-id", message.ConnectionId.ToString() },
                    { "published-at", DateTime.UtcNow.ToString("O") }
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
                "✅ Test connection message published successfully");
            _logger.LogInformation(
                "   Exchange: {Exchange}", _exchangeName);
            _logger.LogInformation(
                "   Routing Key: {RoutingKey}", routingKey);
            _logger.LogInformation(
                "   Connection ID: {ConnectionId}", message.ConnectionId);
            _logger.LogInformation(
                "   Tenant ID: {TenantId}", tenantId);
            _logger.LogInformation(
                "   Database: {Type} @ {Host}:{Port}/{Database}", 
                message.Type, message.Host, message.Port, message.Database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ Error publishing test connection message for tenant {TenantId}, connection {ConnectionId}", 
                tenantId, message?.ConnectionId);
            throw;
        }
    }

    /// <summary>
    /// Create unified queue for a tenant with wildcard routing pattern
    /// </summary>
    public async Task CreateTenantQueueAsync(Guid tenantId)
    {
        try
        {
            await EnsureConnectionAsync();

            // Unified queue name for all job types
            var queueName = $"tenant.{tenantId}.jobs";
            var dlqName = $"tenant.{tenantId}.jobs.dlq";
            var routingPattern = $"backup.*.{tenantId}";

            // Check if queue already exists
            bool queueExists = false;
            try
            {
                await _channel!.QueueDeclarePassiveAsync(queue: queueName);
                queueExists = true;
                _logger.LogDebug("Queue {QueueName} already exists", queueName);
            }
            catch
            {
                // Queue doesn't exist, we'll create it
                _logger.LogDebug("Queue {QueueName} doesn't exist, creating...", queueName);
            }

            if (!queueExists)
            {
                // Declare Dead Letter Queue
                await _channel!.QueueDeclareAsync(
                    queue: dlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // Declare unified jobs queue WITHOUT x-message-ttl to match Worker's declaration
                var queueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", "" }, // Default exchange
                    { "x-dead-letter-routing-key", dlqName }
                };

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs
                );

                _logger.LogInformation("Created unified queue: {QueueName} with DLQ: {DLQ}", queueName, dlqName);
            }

            // Always ensure binding exists (this is idempotent)
            await _channel!.QueueBindAsync(
                queue: queueName,
                exchange: _exchangeName,
                routingKey: routingPattern
            );

            _logger.LogInformation(
                "Ensured queue binding for tenant {TenantId}: {QueueName} -> {Exchange} with pattern: {Pattern}",
                tenantId, queueName, _exchangeName, routingPattern
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating queues for tenant {TenantId}", tenantId);
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
    /// Get unified queue statistics for a tenant
    /// </summary>
    public async Task<QueueStats> GetQueueStatsAsync(Guid tenantId)
    {
        try
        {
            await EnsureConnectionAsync();

            // Unified queue name
            var queueName = $"tenant.{tenantId}.jobs";
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
