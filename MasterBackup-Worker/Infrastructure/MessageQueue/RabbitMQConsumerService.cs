using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MasterBackup_Worker.Infrastructure.MessageQueue;

/// <summary>
/// Background service that consumes messages from RabbitMQ queue
/// </summary>
public class RabbitMQConsumerService : BackgroundService
{
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IWorkerAuthorizationService _authorizationService;
    private readonly IConnectionTestService _connectionTestService;
    private readonly IBackupExecutorService _backupExecutorService;
    private readonly IApiClient _apiClient;
    private readonly WorkerConfiguration _workerConfig;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _unifiedQueueName;
    private readonly string _rabbitMQHost;
    private readonly int _rabbitMQPort;

    public RabbitMQConsumerService(
        ILogger<RabbitMQConsumerService> logger,
        IWorkerAuthorizationService authorizationService,
        IConnectionTestService connectionTestService,
        IBackupExecutorService backupExecutorService,
        IApiClient apiClient,
        WorkerConfiguration workerConfig,
        string rabbitMQHost,
        int rabbitMQPort)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _connectionTestService = connectionTestService ?? throw new ArgumentNullException(nameof(connectionTestService));
        _backupExecutorService = backupExecutorService ?? throw new ArgumentNullException(nameof(backupExecutorService));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _workerConfig = workerConfig ?? throw new ArgumentNullException(nameof(workerConfig));
        _rabbitMQHost = rabbitMQHost;
        _rabbitMQPort = rabbitMQPort;

        _logger.LogInformation("RabbitMQ Consumer service created. Will initialize after worker registration.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for worker to be registered (WorkerId and TenantId will be set by WorkerRegistrationService)
        _logger.LogInformation("Waiting for worker registration to complete...");
        while (_workerConfig.WorkerId == Guid.Empty && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        
        if (_workerConfig.WorkerId == Guid.Empty)
        {
            _logger.LogError("Worker registration failed. RabbitMQ consumer will not start.");
            return;
        }
        
        _logger.LogInformation("Worker registered with ID: {WorkerId}. Obtaining credentials from API...", _workerConfig.WorkerId);
        
        // Get credentials from API
        MasterBackup_Worker.Domain.Models.WorkerCredentialsDto credentials;
        try
        {
            credentials = await _apiClient.GetCredentialsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain credentials from API. RabbitMQ consumer will not start.");
            return;
        }

        // Store Azure Storage connection string in environment (for BackupExecutorService)
        Environment.SetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING", credentials.AzureStorageConnectionString);
        _logger.LogInformation("✓ Azure Storage credentials configured");

        // Use queue name from credentials
        _unifiedQueueName = credentials.RabbitMQ.QueueName;
        
        _logger.LogInformation("Worker will consume from queue: {Queue}", _unifiedQueueName);
        _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}...", credentials.RabbitMQ.Host, credentials.RabbitMQ.Port);
        
        // Initialize RabbitMQ connection with credentials from API
        var factory = new ConnectionFactory
        {
            HostName = credentials.RabbitMQ.Host,
            Port = credentials.RabbitMQ.Port,
            UserName = credentials.RabbitMQ.Username,
            Password = credentials.RabbitMQ.Password,
            VirtualHost = credentials.RabbitMQ.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare unified queue (idempotent)
        _channel.QueueDeclare(
            queue: _unifiedQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Set prefetch count to limit concurrent processing
        _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_workerConfig.MaxConcurrentJobs, global: false);

        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("RabbitMQ Consumer Initialized Successfully");
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("Unified Queue:  {UnifiedQueue}", _unifiedQueueName);
        _logger.LogInformation("Host:        {Host}:{Port}", _rabbitMQHost, _rabbitMQPort);
        _logger.LogInformation("Tenant ID:   {TenantId}", _workerConfig.TenantId);
        _logger.LogInformation("Worker ID:   {WorkerId}", _workerConfig.WorkerId);
        _logger.LogInformation("Max Jobs:    {MaxJobs}", _workerConfig.MaxConcurrentJobs);
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        
        // Single consumer for unified queue
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            await ProcessMessageAsync(ea, _channel, _unifiedQueueName!);
        };

        // Start consuming from unified queue
        _channel?.BasicConsume(
            queue: _unifiedQueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║  ✓ RabbitMQ Consumer ACTIVE - Listening for ALL messages ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
        _logger.LogInformation("Unified Queue: {UnifiedQueue}", _unifiedQueueName);
        _logger.LogInformation("Message Types: BackupJob, TestConnection, and future types");

        // Keep the service running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer stopping...");
        }
        finally
        {
            _channel?.Close();
            _connection?.Close();
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IModel channel, string queueName)
    {
        try
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;
            
            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║  📩 NEW MESSAGE RECEIVED                                  ║");
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
            _logger.LogInformation("Queue:       {QueueName}", queueName);
            _logger.LogInformation("Routing Key: {RoutingKey}", routingKey);
            
            // Extract message type from headers (priority) or routing key (fallback)
            string messageType;
            if (ea.BasicProperties?.Headers != null && 
                ea.BasicProperties.Headers.TryGetValue("message-type", out var headerValue))
            {
                messageType = Encoding.UTF8.GetString((byte[])headerValue);
                _logger.LogInformation("Message Type: {MessageType} (from header)", messageType);
            }
            else
            {
                messageType = DetermineMessageTypeFromRoutingKey(routingKey);
                _logger.LogInformation("Message Type: {MessageType} (from routing key)", messageType);
            }
            
            _logger.LogDebug("Message Content: {Message}", 
                messageJson.Length > 500 ? messageJson.Substring(0, 500) + "..." : messageJson);

            // Process based on message type
            var processed = messageType switch
            {
                "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
                "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
                _ => false
            };
            
            if (processed)
            {
                channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                _logger.LogInformation("✓ Message acknowledged successfully (Type: {MessageType})", messageType);
            }
            else
            {
                // Reject and requeue if not processed (another worker might be able to handle it)
                channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                _logger.LogWarning("⚠ Message not processed, requeued for another worker");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing message from queue {QueueName}", queueName);
            
            // Don't requeue on exception to avoid infinite loops
            channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task<bool> ProcessTestConnectionMessageAsync(string messageJson)
    {
        try
        {
            var message = JsonSerializer.Deserialize<TestConnectionMessage>(messageJson, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize test connection message");
                return false;
            }

            // AUTHORIZATION: Check if this worker can process the test connection
            var isAuthorized = await _authorizationService.CanProcessTestConnectionAsync(message);
            
            if (!isAuthorized)
            {
                var reason = _authorizationService.GetAuthorizationFailureReason();
                _logger.LogWarning("Worker {WorkerId} is not authorized to process test connection {ConnectionId}: {Reason}",
                    _workerConfig.WorkerId, message.ConnectionId, reason);
                
                // Return false to requeue - another worker might be authorized
                return false;
            }

            // Execute test connection
            _logger.LogInformation("Processing test connection for {ConnectionId} on worker {WorkerId}",
                message.ConnectionId, _workerConfig.WorkerId);

            var (success, testMessage) = await _connectionTestService.TestConnectionAsync(message);

            // Notify API of test result
            await _apiClient.NotifyTestConnectionCompletedAsync(message.ConnectionId, success, testMessage);

            _logger.LogInformation("Test connection {ConnectionId} completed: {Success}",
                message.ConnectionId, success ? "SUCCESS" : "FAILED");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing test connection message");
            return false;
        }
    }

    private async Task<bool> ProcessBackupJobMessageAsync(string messageJson)
    {
        try
        {
            var message = JsonSerializer.Deserialize<BackupJobMessage>(messageJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize backup job message");
                return false;
            }

            // AUTHORIZATION: Check if this worker can process the backup job
            var isAuthorized = await _authorizationService.CanProcessBackupJobAsync(message);
            
            if (!isAuthorized)
            {
                var reason = _authorizationService.GetAuthorizationFailureReason();
                _logger.LogWarning("Worker {WorkerId} is not authorized to process backup job {JobId}: {Reason}",
                    _workerConfig.WorkerId, message.JobId, reason);
                
                // Return false to requeue - another worker might be authorized
                return false;
            }

            // Execute backup job
            _logger.LogInformation("Processing backup job {JobId} on worker {WorkerId}",
                message.JobId, _workerConfig.WorkerId);

            // BackupExecutorService now handles all status reporting via IBackupStatusReporter
            await _backupExecutorService.ExecuteBackupAsync(message);
            
            _logger.LogInformation("Backup job {JobId} processing completed",
                message.JobId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing backup job message");
            return false;
        }
    }

    private string DetermineMessageTypeFromRoutingKey(string routingKey)
    {
        _logger.LogDebug("Determining message type from routing key: {RoutingKey}", routingKey);
        
        // Routing key patterns:
        // - backup.execute.{tenantId} -> BackupJob
        // - backup.test.{tenantId} -> TestConnection
        
        if (routingKey.Contains(".execute.", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Message identified as BackupJob (routing key contains .execute.)");
            return "BackupJob";
        }
        
        if (routingKey.Contains(".test.", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Message identified as TestConnection (routing key contains .test.)");
            return "TestConnection";
        }

        _logger.LogWarning("Unknown message type. Routing key does not match expected patterns: {RoutingKey}", routingKey);
        return "Unknown";
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
