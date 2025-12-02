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
    private string? _queueName;
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
        
        _logger.LogInformation("Worker registered with ID: {WorkerId}. Initializing RabbitMQ connection...", _workerConfig.WorkerId);
        
        // NOW we can determine the queue name with the correct TenantId
        // Listen to backup jobs queue
        _queueName = $"backup.jobs.{_workerConfig.TenantId}";
        
        _logger.LogInformation("Worker will consume from queue: {QueueName}", _queueName);
        
        // Initialize RabbitMQ connection
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMQHost,
            Port = _rabbitMQPort,
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
        _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_workerConfig.MaxConcurrentJobs, global: false);

        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("RabbitMQ Consumer Initialized Successfully");
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        _logger.LogInformation("Queue Name:  {QueueName}", _queueName);
        _logger.LogInformation("Host:        {Host}:{Port}", _rabbitMQHost, _rabbitMQPort);
        _logger.LogInformation("Tenant ID:   {TenantId}", _workerConfig.TenantId);
        _logger.LogInformation("Worker ID:   {WorkerId}", _workerConfig.WorkerId);
        _logger.LogInformation("Max Jobs:    {MaxJobs}", _workerConfig.MaxConcurrentJobs);
        _logger.LogInformation("═══════════════════════════════════════════════════════════");
        
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
                _logger.LogInformation("║  📩 NEW MESSAGE RECEIVED from queue {QueueName}", _queueName);
                _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
                _logger.LogInformation("Message Content: {Message}", messageJson);

                // Determine message type and process accordingly
                var messageType = DetermineMessageType(messageJson);
                _logger.LogInformation("Message type determined: {MessageType}", messageType);
                
                var processed = messageType switch
                {
                    "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
                    "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
                    _ => false
                };
                
                _logger.LogInformation("Message processed: {Processed}", processed);

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

        _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║  ✓ RabbitMQ Consumer ACTIVE - Listening for messages     ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
        _logger.LogInformation("Waiting for messages on queue: {QueueName}...", _queueName);

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

    private string DetermineMessageType(string messageJson)
    {
        _logger.LogDebug("Determining message type for: {Message}", messageJson);
        
        // Simple heuristic: check if message contains JobId and DatabaseConnection (BackupJob)
        // or ConnectionId (TestConnection)
        if (messageJson.Contains("\"JobId\"", StringComparison.OrdinalIgnoreCase) && 
            messageJson.Contains("\"DatabaseConnection\"", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Message identified as BackupJob (contains JobId and DatabaseConnection)");
            return "BackupJob";
        }
        
        if (messageJson.Contains("\"ConnectionId\"", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Message identified as TestConnection (contains ConnectionId)");
            return "TestConnection";
        }

        _logger.LogWarning("Unknown message type. Message does not contain expected fields for BackupJob or TestConnection");
        return "Unknown";
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
