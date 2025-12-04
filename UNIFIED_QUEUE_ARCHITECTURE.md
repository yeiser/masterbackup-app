# Unified Queue Architecture with RabbitMQ Topic Exchange

## Overview

The MasterBackup system uses a **unified queue architecture** with RabbitMQ topic exchange and routing keys to handle different message types (backup jobs, connection tests, etc.) through a single queue per tenant.

This approach provides:
- ✅ Simplified queue management (one queue per tenant)
- ✅ Better scalability (leverages RabbitMQ native features)
- ✅ Clear message routing through routing keys
- ✅ Easy to extend with new message types

---

## Architecture Components

### Exchange Configuration
- **Name**: `masterbackup.jobs`
- **Type**: `topic` (supports wildcard routing)
- **Durable**: `true` (survives broker restart)

### Queue Naming Convention
- **Pattern**: `tenant.{tenantId}.jobs`
- **Examples**:
  - `tenant.1.jobs`
  - `tenant.2.jobs`
  - `tenant.abc123.jobs`

### Routing Key Patterns
- **Backup Execution**: `backup.execute.{tenantId}`
- **Connection Test**: `backup.test.{tenantId}`
- **Future (Restore)**: `backup.restore.{tenantId}`
- **Future (Verify)**: `backup.verify.{tenantId}`

### Queue Binding
- Each tenant queue binds to exchange with pattern: `backup.*.{tenantId}`
- This wildcard binding catches all backup-related messages for that tenant

---

## Message Flow

### 1. API Publishes Message

```csharp
// Example: Execute Backup
await _rabbitMQService.PublishBackupJobAsync(backupJob);

// Internally:
// - Exchange: masterbackup.jobs
// - Routing Key: backup.execute.{tenantId}
// - Headers: message-type = "BackupJob"
```

### 2. RabbitMQ Routes Message

```
Exchange (masterbackup.jobs)
    ↓
Evaluates routing key: backup.execute.1
    ↓
Matches binding pattern: backup.*.1
    ↓
Routes to queue: tenant.1.jobs
```

### 3. Worker Consumes Message

```csharp
// Worker listens to: tenant.{tenantId}.jobs
// Receives message with:
// - Routing Key: backup.execute.1
// - Headers: { message-type: "BackupJob" }
// - Body: JSON payload

// Determines message type from:
// 1. Headers (priority): message-type = "BackupJob"
// 2. Routing key (fallback): contains ".execute." = BackupJob

// Routes to appropriate handler:
// - BackupJob → BackupExecutorService
// - TestConnection → ConnectionTestService
```

---

## Code Implementation

### API - Publishing Messages

**Location**: `MasterBackup-API/Infrastructure/Services/RabbitMQService.cs`

```csharp
public async Task PublishBackupJobAsync(BackupJobMessage message)
{
    var routingKey = $"backup.execute.{message.TenantId}";
    
    var properties = _channel.CreateBasicProperties();
    properties.Persistent = true;
    properties.Headers = new Dictionary<string, object>
    {
        { "message-type", "BackupJob" },
        { "tenant-id", message.TenantId },
        { "published-at", DateTime.UtcNow.ToString("O") }
    };

    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
    
    _channel.BasicPublish(
        exchange: _exchangeName,        // masterbackup.jobs
        routingKey: routingKey,         // backup.execute.1
        basicProperties: properties,
        body: body
    );
}
```

### API - Creating Tenant Queue

**Location**: `MasterBackup-API/Infrastructure/Services/RabbitMQService.cs`

```csharp
public async Task CreateTenantQueueAsync(string tenantId)
{
    var unifiedQueueName = $"tenant.{tenantId}.jobs";
    var dlqName = $"tenant.{tenantId}.jobs.dlq";
    var routingPattern = $"backup.*.{tenantId}";

    // Declare dead letter queue
    _channel.QueueDeclare(
        queue: dlqName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null
    );

    // Declare main queue with DLQ
    var queueArgs = new Dictionary<string, object>
    {
        { "x-dead-letter-exchange", _exchangeName },
        { "x-dead-letter-routing-key", $"backup.dlq.{tenantId}" }
    };

    _channel.QueueDeclare(
        queue: unifiedQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: queueArgs
    );

    // Bind queue to exchange with wildcard pattern
    _channel.QueueBind(
        queue: unifiedQueueName,
        exchange: _exchangeName,
        routingKey: routingPattern  // backup.*.{tenantId}
    );
}
```

### Worker - Consuming Messages

**Location**: `MasterBackup-Worker/Infrastructure/MessageQueue/RabbitMQConsumerService.cs`

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var unifiedQueueName = $"tenant.{_workerConfig.TenantId}.jobs";
    
    _channel = _connection.CreateModel();
    _channel.QueueDeclare(
        queue: unifiedQueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null
    );

    var consumer = new EventingBasicConsumer(_channel);
    consumer.Received += async (model, ea) =>
    {
        await ProcessMessageAsync(ea, _channel, unifiedQueueName);
    };

    _channel.BasicConsume(
        queue: unifiedQueueName,
        autoAck: false,
        consumer: consumer
    );
}
```

### Worker - Message Type Detection

**Location**: `MasterBackup-Worker/Infrastructure/MessageQueue/RabbitMQConsumerService.cs`

```csharp
private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IModel channel, string queueName)
{
    var routingKey = ea.RoutingKey;
    
    // Extract message type from headers (priority) or routing key (fallback)
    string messageType;
    if (ea.BasicProperties?.Headers != null && 
        ea.BasicProperties.Headers.TryGetValue("message-type", out var headerValue))
    {
        messageType = Encoding.UTF8.GetString((byte[])headerValue);
    }
    else
    {
        messageType = DetermineMessageTypeFromRoutingKey(routingKey);
    }
    
    // Route to appropriate handler
    var processed = messageType switch
    {
        "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
        "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
        _ => false
    };
}

private string DetermineMessageTypeFromRoutingKey(string routingKey)
{
    if (routingKey.Contains(".execute.")) return "BackupJob";
    if (routingKey.Contains(".test.")) return "TestConnection";
    return "Unknown";
}
```

---

## Message Types

### BackupJob Message

**Routing Key**: `backup.execute.{tenantId}`

**Payload**:
```json
{
  "JobId": "550e8400-e29b-41d4-a716-446655440000",
  "TenantId": "1",
  "BackupHistoryId": 123,
  "DatabaseConnection": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "mydb",
    "Username": "user",
    "Password": "encrypted_password"
  },
  "BackupType": "Full",
  "RetentionDays": 30,
  "ContainerName": "tenant-1-backups"
}
```

**Handler**: `BackupExecutorService.ExecuteBackupAsync()`

### TestConnection Message

**Routing Key**: `backup.test.{tenantId}`

**Payload**:
```json
{
  "ConnectionId": 456,
  "TenantId": "1",
  "DatabaseConnection": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "testdb",
    "Username": "user",
    "Password": "encrypted_password"
  }
}
```

**Handler**: `ConnectionTestService.TestConnectionAsync()`

---

## Adding New Message Types

To add a new message type (e.g., backup restore):

### 1. Define Routing Key Pattern
```csharp
// backup.restore.{tenantId}
```

### 2. Update API Publisher
```csharp
public async Task PublishBackupRestoreAsync(BackupRestoreMessage message)
{
    var routingKey = $"backup.restore.{message.TenantId}";
    
    var properties = _channel.CreateBasicProperties();
    properties.Persistent = true;
    properties.Headers = new Dictionary<string, object>
    {
        { "message-type", "BackupRestore" },
        { "tenant-id", message.TenantId }
    };
    
    // ... publish logic
}
```

### 3. Update Worker Message Type Detection
```csharp
private string DetermineMessageTypeFromRoutingKey(string routingKey)
{
    if (routingKey.Contains(".execute.")) return "BackupJob";
    if (routingKey.Contains(".test.")) return "TestConnection";
    if (routingKey.Contains(".restore.")) return "BackupRestore";  // NEW
    return "Unknown";
}
```

### 4. Add Worker Handler
```csharp
var processed = messageType switch
{
    "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
    "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
    "BackupRestore" => await ProcessBackupRestoreMessageAsync(messageJson),  // NEW
    _ => false
};
```

**No changes needed** to queue binding pattern (`backup.*.{tenantId}`) - it already catches all backup-related messages!

---

## Monitoring & Troubleshooting

### Verify Queue Setup

**Check RabbitMQ Management UI** (http://localhost:15672):
1. Navigate to "Exchanges" → `masterbackup.jobs`
2. Verify Type = "topic"
3. Check bindings to tenant queues

**Check Queue Bindings**:
```
Queue: tenant.1.jobs
Binding: backup.*.1 → masterbackup.jobs
```

### Worker Logs

When processing a message, Worker logs show:

```
╔═══════════════════════════════════════════════════════════╗
║  📩 NEW MESSAGE RECEIVED                                  ║
╚═══════════════════════════════════════════════════════════╝
Queue:       tenant.1.jobs
Routing Key: backup.execute.1
Message Type: BackupJob (from header)
```

### Common Issues

#### Issue: Messages not routing to queue
**Symptom**: Messages published but Worker doesn't receive them

**Check**:
1. Exchange exists and is type "topic"
2. Queue binding pattern matches routing key
3. Routing key format: `backup.{action}.{tenantId}`

**Fix**:
```bash
# Delete and recreate queue with correct binding
docker exec rabbitmq rabbitmqctl delete_queue tenant.1.jobs
# Restart API to recreate queue
```

#### Issue: Worker can't determine message type
**Symptom**: Logs show "Unknown message type"

**Check**:
1. API sets message-type header
2. Routing key follows pattern
3. Worker routing key detection logic

**Debug**:
```csharp
_logger.LogDebug("Headers: {@Headers}", ea.BasicProperties.Headers);
_logger.LogDebug("Routing Key: {RoutingKey}", ea.RoutingKey);
```

#### Issue: Worker not starting
**Symptom**: Worker crashes on startup

**Check**:
1. RabbitMQ connection string correct
2. TenantId configured in Worker appsettings.json
3. Queue declared successfully

**Logs to check**:
```
🔌 ESTABLISHING RABBITMQ CONNECTION
✓ RabbitMQ connection established
✓ Queue tenant.1.jobs declared successfully
```

---

## Performance Considerations

### Prefetch Count
Worker prefetches 10 messages by default:

```csharp
_channel.BasicQos(0, 10, false);
```

- **Increase** for faster processing (more memory)
- **Decrease** for better distribution across workers

### Message Acknowledgment
Messages are acknowledged only after successful processing:

```csharp
if (processed)
{
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
}
else
{
    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
}
```

### Dead Letter Queue (DLQ)
Failed messages automatically go to DLQ after max retries:
- Queue: `tenant.{tenantId}.jobs.dlq`
- Routing: `backup.dlq.{tenantId}`

---

## Comparison with Previous Architecture

### Before (Dual Queue Strategy)
```
API publishes to:
- backup.jobs.{tenantId}  (for BackupJob)
- {tenantId}.test-connection.queue  (for TestConnection)

Worker consumes from:
- Two separate queues
- Two separate channels
- Complex channel management
```

### After (Unified Queue Strategy)
```
API publishes to:
- masterbackup.jobs exchange with routing keys:
  - backup.execute.{tenantId}
  - backup.test.{tenantId}

Worker consumes from:
- Single queue: tenant.{tenantId}.jobs
- Single channel
- Message type from headers/routing key
```

**Benefits**:
- 50% fewer queues to manage
- 50% fewer channels in Worker
- Easier to add new message types
- Better scalability
- Leverages RabbitMQ topic exchange features

---

## References

- [RabbitMQ Topic Exchange Tutorial](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)
- [RabbitMQ Routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet.html)
- [Dead Letter Exchanges](https://www.rabbitmq.com/dlx.html)
