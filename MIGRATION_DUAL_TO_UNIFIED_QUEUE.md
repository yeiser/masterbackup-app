# Migration: Dual Queue → Unified Queue Architecture

## Summary

Successfully migrated from **dual queue strategy** (separate queues per message type) to **unified queue with routing keys** (single queue per tenant with RabbitMQ topic exchange).

**Date**: December 2024  
**Impact**: API + Worker  
**Status**: ✅ Complete - Ready for Testing

---

## Changes Made

### 1. API - RabbitMQService.cs

#### Exchange Configuration
```diff
- _exchangeName = "masterbackup.exchange"
+ _exchangeName = "masterbackup.jobs"
+ _exchangeType = "topic"
```

#### PublishBackupJobAsync
```diff
- routingKey = $"backup.jobs.{message.TenantId}"
+ routingKey = $"backup.execute.{message.TenantId}"
+ 
+ properties.Headers = new Dictionary<string, object>
+ {
+     { "message-type", "BackupJob" },
+     { "tenant-id", message.TenantId },
+     { "published-at", DateTime.UtcNow.ToString("O") }
+ };
```

#### PublishTestConnectionAsync
```diff
- routingKey = $"{message.TenantId}.test-connection.queue"
+ routingKey = $"backup.test.{message.TenantId}"
+ 
+ properties.Headers = new Dictionary<string, object>
+ {
+     { "message-type", "TestConnection" },
+     { "tenant-id", message.TenantId },
+     { "published-at", DateTime.UtcNow.ToString("O") }
+ };
```

#### CreateTenantQueueAsync
```diff
- // Created two separate queues:
- var backupJobsQueueName = $"backup.jobs.{tenantId}";
- var backupResultsQueueName = $"backup.results.{tenantId}";
- var testConnectionQueueName = $"{tenantId}.test-connection.queue";

+ // Creates single unified queue:
+ var unifiedQueueName = $"tenant.{tenantId}.jobs";
+ var dlqName = $"tenant.{tenantId}.jobs.dlq";
+ var routingPattern = $"backup.*.{tenantId}";
+ 
+ _channel.QueueBind(
+     queue: unifiedQueueName,
+     exchange: _exchangeName,
+     routingKey: routingPattern  // Wildcard binding
+ );
```

---

### 2. Worker - RabbitMQConsumerService.cs

#### Channel and Queue Variables
```diff
- private IModel? _channelBackupJobs;
- private IModel? _channelTestConnection;
- private string _backupJobsQueueName = string.Empty;
- private string _testConnectionQueueName = string.Empty;

+ private IModel? _channel;
+ private string _unifiedQueueName = string.Empty;
```

#### ExecuteAsync - Queue Setup
```diff
- // Created two channels:
- _channelBackupJobs = _connection.CreateModel();
- _channelTestConnection = _connection.CreateModel();
- 
- _backupJobsQueueName = $"backup.jobs.{_workerConfig.TenantId}";
- _testConnectionQueueName = $"{_workerConfig.TenantId}.test-connection.queue";

+ // Single channel and queue:
+ _channel = _connection.CreateModel();
+ _unifiedQueueName = $"tenant.{_workerConfig.TenantId}.jobs";
+ 
+ _channel.QueueDeclare(
+     queue: _unifiedQueueName,
+     durable: true,
+     exclusive: false,
+     autoDelete: false,
+     arguments: null
+ );
```

#### ExecuteAsync - Consumer Setup
```diff
- // Two separate consumers:
- var consumerBackupJobs = new EventingBasicConsumer(_channelBackupJobs);
- consumerBackupJobs.Received += async (model, ea) => { ... };
- _channelBackupJobs.BasicConsume(_backupJobsQueueName, ...);
- 
- var consumerTestConnection = new EventingBasicConsumer(_channelTestConnection);
- consumerTestConnection.Received += async (model, ea) => { ... };
- _channelTestConnection.BasicConsume(_testConnectionQueueName, ...);

+ // Single consumer:
+ var consumer = new EventingBasicConsumer(_channel);
+ consumer.Received += async (model, ea) =>
+ {
+     await ProcessMessageAsync(ea, _channel, _unifiedQueueName);
+ };
+ 
+ _channel.BasicConsume(
+     queue: _unifiedQueueName,
+     autoAck: false,
+     consumer: consumer
+ );
```

#### ProcessMessageAsync - Message Type Detection
```diff
- private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IModel channel, string queueName, string queueType)
+ private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, IModel channel, string queueName)
  {
+     var routingKey = ea.RoutingKey;
+     
+     // Extract message type from headers (priority) or routing key (fallback)
+     string messageType;
+     if (ea.BasicProperties?.Headers != null && 
+         ea.BasicProperties.Headers.TryGetValue("message-type", out var headerValue))
+     {
+         messageType = Encoding.UTF8.GetString((byte[])headerValue);
+     }
+     else
+     {
+         messageType = DetermineMessageTypeFromRoutingKey(routingKey);
+     }
      
-     var messageType = DetermineMessageType(messageJson);
      
      var processed = messageType switch
      {
          "TestConnection" => await ProcessTestConnectionMessageAsync(messageJson),
          "BackupJob" => await ProcessBackupJobMessageAsync(messageJson),
          _ => false
      };
  }
```

#### DetermineMessageType Method
```diff
- private string DetermineMessageType(string messageJson)
- {
-     // Checked JSON content for JobId/ConnectionId fields
-     if (messageJson.Contains("\"JobId\"") && messageJson.Contains("\"DatabaseConnection\""))
-         return "BackupJob";
-     
-     if (messageJson.Contains("\"ConnectionId\""))
-         return "TestConnection";
-     
-     return "Unknown";
- }

+ private string DetermineMessageTypeFromRoutingKey(string routingKey)
+ {
+     // Checks routing key pattern
+     if (routingKey.Contains(".execute."))
+         return "BackupJob";
+     
+     if (routingKey.Contains(".test."))
+         return "TestConnection";
+     
+     return "Unknown";
+ }
```

#### Dispose Method
```diff
  public override void Dispose()
  {
-     _channelBackupJobs?.Close();
-     _channelTestConnection?.Close();
+     _channel?.Close();
      _connection?.Close();
      base.Dispose();
  }
```

---

## Benefits of New Architecture

### Before (Dual Queue)
- ❌ Multiple queues per tenant (backup.jobs.{tenantId}, {tenantId}.test-connection.queue)
- ❌ Multiple channels in Worker (complex management)
- ❌ Different queue naming patterns (inconsistent)
- ❌ Hard to extend with new message types
- ❌ More RabbitMQ resources consumed

### After (Unified Queue)
- ✅ Single queue per tenant (tenant.{tenantId}.jobs)
- ✅ Single channel in Worker (simplified)
- ✅ Consistent queue naming pattern
- ✅ Easy to add new message types (just add new routing key)
- ✅ Leverages RabbitMQ topic exchange features
- ✅ Better scalability

---

## Routing Key Patterns

| Message Type | Routing Key | Handler |
|--------------|-------------|---------|
| Backup Execution | `backup.execute.{tenantId}` | BackupExecutorService |
| Connection Test | `backup.test.{tenantId}` | ConnectionTestService |
| Future: Restore | `backup.restore.{tenantId}` | TBD |
| Future: Verify | `backup.verify.{tenantId}` | TBD |

**Queue Binding**: `backup.*.{tenantId}` (catches all backup-related messages)

---

## Testing Checklist

### Pre-Testing Setup
- [ ] Ensure RabbitMQ is running
- [ ] Clear old queues from RabbitMQ (if needed)
- [ ] Set AZURE_STORAGE_CONNECTION_STRING environment variable
- [ ] Configure Worker appsettings.json with correct TenantId

### API Testing
- [ ] Start API
- [ ] Check logs for exchange declaration:
  ```
  ✓ Exchange 'masterbackup.jobs' (type: topic) declared successfully
  ```
- [ ] Execute instant backup from frontend
- [ ] Check logs for message publish:
  ```
  📤 Published message to exchange 'masterbackup.jobs' with routing key 'backup.execute.1'
  ```

### Worker Testing
- [ ] Start Worker
- [ ] Check logs for connection and queue setup:
  ```
  🔌 ESTABLISHING RABBITMQ CONNECTION
  ✓ RabbitMQ connection established
  ✓ Queue tenant.1.jobs declared successfully
  🎧 Started listening to queue: tenant.1.jobs
  ```
- [ ] Trigger backup from frontend
- [ ] Verify Worker receives message:
  ```
  ╔═══════════════════════════════════════════════════════════╗
  ║  📩 NEW MESSAGE RECEIVED                                  ║
  ╚═══════════════════════════════════════════════════════════╝
  Queue:       tenant.1.jobs
  Routing Key: backup.execute.1
  Message Type: BackupJob (from header)
  ```
- [ ] Verify backup execution completes successfully
- [ ] Check Azure Storage for backup file

### Connection Test Testing
- [ ] Trigger connection test from frontend
- [ ] Check Worker logs for test message:
  ```
  Queue:       tenant.1.jobs
  Routing Key: backup.test.1
  Message Type: TestConnection (from header)
  ```
- [ ] Verify connection test completes
- [ ] Check API receives test result notification

### RabbitMQ Management UI
- [ ] Open http://localhost:15672
- [ ] Navigate to Exchanges → `masterbackup.jobs`
- [ ] Verify Type = "topic"
- [ ] Check bindings show `tenant.1.jobs` with pattern `backup.*.1`
- [ ] Navigate to Queues → `tenant.1.jobs`
- [ ] Verify messages are being consumed

---

## Rollback Plan (If Needed)

If issues arise, rollback by reverting these files:

### API
```bash
git checkout HEAD~1 MasterBackup-API/Infrastructure/Services/RabbitMQService.cs
```

### Worker
```bash
git checkout HEAD~1 MasterBackup-Worker/Infrastructure/MessageQueue/RabbitMQConsumerService.cs
```

Then restart both API and Worker.

---

## Next Steps

1. ✅ Complete implementation (DONE)
2. ⏳ Test end-to-end flow (NEXT)
3. ⏳ Monitor Worker logs for any issues
4. ⏳ Test with multiple tenants (if applicable)
5. ⏳ Update monitoring dashboards (if any)
6. ⏳ Consider adding metrics for routing key patterns

---

## Future Enhancements

### 1. Backup Restore
Add routing key: `backup.restore.{tenantId}`

```csharp
public async Task PublishBackupRestoreAsync(BackupRestoreMessage message)
{
    var routingKey = $"backup.restore.{message.TenantId}";
    // ... publish logic
}
```

### 2. Backup Verification
Add routing key: `backup.verify.{tenantId}`

```csharp
public async Task PublishBackupVerifyAsync(BackupVerifyMessage message)
{
    var routingKey = $"backup.verify.{message.TenantId}";
    // ... publish logic
}
```

### 3. Priority Queue
Use RabbitMQ priority queue feature:

```csharp
var queueArgs = new Dictionary<string, object>
{
    { "x-max-priority", 10 }
};
```

Then set message priority:
```csharp
properties.Priority = 5; // 0-10
```

### 4. Message TTL
Add time-to-live for messages:

```csharp
properties.Expiration = "60000"; // 60 seconds in milliseconds
```

---

## Documentation

- **Architecture**: See `UNIFIED_QUEUE_ARCHITECTURE.md`
- **Backup Processing**: See `BACKUP_PROCESSING_FLOW.md`
- **Azure Storage**: See `AZURE_STORAGE_CONFIGURATION.md`

---

## Notes

- No database migrations needed (only RabbitMQ changes)
- No frontend changes needed (API endpoints unchanged)
- Worker authorization logic unchanged
- Backup execution flow unchanged
- Status reporting unchanged

The refactoring only affects the **message queuing layer** - everything else remains the same.
