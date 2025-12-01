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
    private readonly string _queueName;

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

        // Queue name format: {tenantId}.test-connection.queue
        // Note: This queue is used for both test connections and backup jobs
        _queueName = $"{workerConfig.TenantId}.test-connection.queue";

        // Initialize RabbitMQ connection
        var factory = new ConnectionFactory
        {
            HostName = rabbitMQHost,
            Port = rabbitMQPort,
            UserName = "guest",
            Password = "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare queue (idempotent)
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Set prefetch count to limit concurrent processing
        _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)workerConfig.MaxConcurrentJobs, global: false);

        _logger.LogInformation("RabbitMQ consumer initialized for queue: {QueueName}", _queueName);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Received message from queue {QueueName}: {Message}", 
                    _queueName, messageJson);

                // Determine message type and process accordingly
                var messageType = DetermineMessageType(messageJson);
                
                var processed = messageType switch
                {
                    "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
                    "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
                    _ => false
                };

                if (processed)
                {
                    _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("Message acknowledged successfully");
                }
                else
                {
                    // Reject and requeue if not processed (another worker might be able to handle it)
                    _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    _logger.LogWarning("Message not processed, requeued for another worker");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", _queueName);
                
                // Don't requeue on exception to avoid infinite loops
                _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel?.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started listening on queue: {QueueName}", _queueName);

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
                _logger.LogWarning("Worker {WorkerId} is not authorized to process backup job {BackupExecutionId}: {Reason}",
                    _workerConfig.WorkerId, message.BackupExecutionId, reason);
                
                // Return false to requeue - another worker might be authorized
                return false;
            }

            // Execute backup job
            _logger.LogInformation("Processing backup job {BackupExecutionId} on worker {WorkerId}",
                message.BackupExecutionId, _workerConfig.WorkerId);

            // Update status to InProgress
            await _apiClient.UpdateBackupExecutionStatusAsync(message.BackupExecutionId, "InProgress");

            var (success, filePath, fileSize, backupMessage) = await _backupExecutorService.ExecuteBackupAsync(message);

            if (success)
            {
                await _apiClient.UpdateBackupExecutionStatusAsync(message.BackupExecutionId, "Completed");
                await _apiClient.UploadBackupFileMetadataAsync(message.BackupExecutionId, filePath, filePath, fileSize);
                
                _logger.LogInformation("Backup job {BackupExecutionId} completed successfully",
                    message.BackupExecutionId);
            }
            else
            {
                await _apiClient.UpdateBackupExecutionStatusAsync(message.BackupExecutionId, "Failed", backupMessage);
                
                _logger.LogWarning("Backup job {BackupExecutionId} failed: {Message}",
                    message.BackupExecutionId, backupMessage);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing backup job message");
            return false;
        }
    }

    private string DetermineMessageType(string messageJson)
    {
        // Simple heuristic: check if message contains BackupExecutionId or ConnectionId
        if (messageJson.Contains("BackupExecutionId", StringComparison.OrdinalIgnoreCase))
            return "BackupJob";
        
        if (messageJson.Contains("ConnectionId", StringComparison.OrdinalIgnoreCase))
            return "TestConnection";

        return "Unknown";
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
