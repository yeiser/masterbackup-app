# Worker Backup Processing Flow - Implementation Complete

## 🎯 Overview

The Worker is now configured to consume backup job messages from RabbitMQ and execute database backups with full progress reporting to the API.

## 📋 Changes Implemented

### 1. RabbitMQConsumerService Updated

**File:** `Infrastructure/MessageQueue/RabbitMQConsumerService.cs`

#### Queue Configuration
- **Dual Queue Consumption**: Worker now listens to BOTH queues simultaneously
  - Backup Jobs Queue: `backup.jobs.{tenantId}`
  - Test Connection Queue: `{tenantId}.test-connection.queue`
- Example queues:
  - `backup.jobs.ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b`
  - `ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b.test-connection.queue`
- Uses separate channels for each queue for better isolation
- Independent prefetch count per queue

#### Message Type Detection
Updated `DetermineMessageType()` method to properly identify BackupJob messages:
- Looks for `"JobId"` AND `"DatabaseConnection"` properties
- More accurate than previous implementation

**Before:**
```csharp
if (messageJson.Contains("BackupExecutionId", StringComparison.OrdinalIgnoreCase))
```

**After:**
```csharp
if (messageJson.Contains("\"JobId\"", StringComparison.OrdinalIgnoreCase) && 
    messageJson.Contains("\"DatabaseConnection\"", StringComparison.OrdinalIgnoreCase))
```

## 🔄 Complete Backup Flow

### Phase 1: API Publishes Job
1. API creates `BackupHistory` record with status `Pending`
2. Generates unique `JobId`
3. Builds `BackupJobMessage` with:
   - Database connection details
   - Azure Blob Storage connection string
   - Container name and blob file name
   - Timeout and retry configuration
4. Publishes message to RabbitMQ queue: `backup.jobs.{tenantId}`
5. Sends SignalR notification: `BackupStarted`

### Phase 2: Worker Consumes Job
1. **RabbitMQConsumerService** receives message from queue
2. Deserializes into `BackupJobMessage`
3. Validates worker authorization via `IWorkerAuthorizationService`
4. Passes to `BackupExecutorService`

### Phase 3: Backup Execution
**Service:** `BackupExecutorService`

#### Step 1: Start Backup (0%)
- Reports status to API: `BackupHistory.Status = InProgress`
- Logs start time

#### Step 2: Database Dump (25%)
- Executes `pg_dump` for PostgreSQL databases
- Outputs to temporary file: `{guid}.sql`
- Reports progress: 25%
- Message: "Executing database dump"

#### Step 3: Compression (50%)
- Compresses SQL file using GZip
- Output: `{guid}.sql.gz`
- Reports progress: 50%
- Message: "Compressing backup file"
- Logs compression ratio

#### Step 4: Upload to Azure (75%)
- Connects to Azure Blob Storage
- Uploads compressed file to container
- Container: `backups-{tenantId}`
- Blob name: `instant_{dbtype}_{dbname}_{timestamp}_{jobid}.sql.gz`
- Reports progress: 75%
- Message: "Uploading to blob storage"

#### Step 5: Completion (100%)
- Reports success to API with metadata:
  - Blob URL
  - File size
  - Duration
  - Compression ratio
- Updates `BackupHistory`:
  - Status = `Completed`
  - EndTime = now
  - FileSize = compressed size
  - BlobUrl = Azure blob URL
- Sends SignalR notification: `BackupCompleted`
- Cleans up temporary files

### Phase 4: Error Handling
If any step fails:
1. Catches exception
2. Reports failure to API with:
   - Error message
   - Error type
   - Stack trace
   - Current retry count
   - Should retry flag
   - Next retry time (if applicable)
3. Updates `BackupHistory.Status = Failed`
4. Sends SignalR notification: `BackupFailed`
5. Cleans up temporary files
6. NACK message (no requeue to avoid loops)

## 📊 Status Reporting

### API Endpoints Called by Worker

**Base URL:** `{ApiBaseUrl}/api/backuphistory`

#### 1. Report Started
```http
POST /api/backuphistory/{jobId}/started
Headers: X-API-Key: {WorkerApiKey}
Body: { "tenantId": "{guid}", "startTime": "2025-12-02T10:20:00Z" }
```

#### 2. Report Progress
```http
POST /api/backuphistory/{jobId}/progress
Body: {
  "tenantId": "{guid}",
  "progressPercentage": 50,
  "statusMessage": "Compressing backup file",
  "processedBytes": 1048576,
  "totalBytes": 2097152
}
```

#### 3. Report Completed
```http
POST /api/backuphistory/{jobId}/completed
Body: {
  "tenantId": "{guid}",
  "blobUrl": "https://storage.azure.com/...",
  "fileName": "instant_postgresql_mydb_20251202_102000_guid.sql.gz",
  "fileSize": 1048576,
  "compressionType": "GZIP",
  "metadata": {
    "database_name": "mydb",
    "database_type": "PostgreSQL",
    "duration_seconds": "45.23",
    "original_size_bytes": "2097152",
    "compressed_size_bytes": "1048576",
    "compression_ratio": "50.00%"
  }
}
```

#### 4. Report Failed
```http
POST /api/backuphistory/{jobId}/failed
Body: {
  "tenantId": "{guid}",
  "errorMessage": "pg_dump failed with exit code 1",
  "errorType": "InvalidOperationException",
  "stackTrace": "...",
  "retryCount": 0,
  "willRetry": true,
  "nextRetryAt": "2025-12-02T10:25:00Z"
}
```

## 🔧 Worker Configuration

### appsettings.json
```json
{
  "Worker": {
    "WorkerName": "Worker-01",
    "ApiKey": "your-api-key-here",
    "Tags": ["production", "high-priority"],
    "SupportedDatabaseTypes": ["PostgreSQL", "MySQL", "SQLServer"],
    "MaxConcurrentJobs": 3
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": "5672"
  },
  "Api": {
    "BaseUrl": "http://localhost:7000"
  }
}
```

### Environment Variables
```bash
# Worker Configuration
WORKER__WORKERNAME=Worker-01
WORKER__APIKEY=your-api-key-here
WORKER__MAXCONCURRENTJOBS=3

# RabbitMQ Configuration
RABBITMQ__HOST=localhost
RABBITMQ__PORT=5672

# API Configuration
API__BASEURL=http://localhost:7000
```

## 🐳 Docker Deployment

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Install PostgreSQL client tools
RUN apt-get update && \
    apt-get install -y postgresql-client && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY bin/Release/net8.0/publish/ .

ENTRYPOINT ["dotnet", "MasterBackup-Worker.dll"]
```

### docker-compose.yml
```yaml
services:
  worker:
    build: ./MasterBackup-Worker
    environment:
      WORKER__APIKEY: ${WORKER_API_KEY}
      WORKER__WORKERNAME: Worker-Docker-01
      RABBITMQ__HOST: rabbitmq
      API__BASEURL: http://api:8080
    depends_on:
      - rabbitmq
      - api
    restart: unless-stopped
```

## 🧪 Testing the Flow

### 1. Start Worker
```bash
cd MasterBackup-Worker
dotnet run
```

Expected output:
```
╔══════════════════════════════════════════════════════════════╗
║          MasterBackup Worker - Starting Up                   ║
╚══════════════════════════════════════════════════════════════╝

Worker will consume from queues:
  - Backup Jobs:      backup.jobs.ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
  - Test Connection:  ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b.test-connection.queue

RabbitMQ Consumer Initialized Successfully
Backup Jobs Queue:      backup.jobs.ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
Test Connection Queue:  ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b.test-connection.queue
Tenant ID:   ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
Worker ID:   550e8400-e29b-41d4-a716-446655440000

✓ RabbitMQ Consumers ACTIVE - Listening for messages
Waiting for messages on:
  - backup.jobs.ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
  - ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b.test-connection.queue
```

### 2. Trigger Instant Backup from Frontend
- Navigate to Backup Schedules
- Click "Ejecutar Ahora" on any schedule
- Confirm execution

### 3. Watch Worker Logs

**For Backup Job:**
```
╔═══════════════════════════════════════════════════════════╗
║  📩 NEW MESSAGE from BackupJobs queue
╚═══════════════════════════════════════════════════════════╝
Queue: backup.jobs.ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
Message type determined: BackupJob
Processing backup job {JobId} on worker {WorkerId}
Starting backup execution for Job {JobId}, Database: {DatabaseName}
Executing postgresql dump to {OutputFile}
Database dump completed. Size: 2.5 MB
Backup compressed. Original: 2.5 MB, Compressed: 0.8 MB
Backup uploaded to blob storage: https://...
Backup Job {JobId} completed successfully in 00:00:45
✓ Message acknowledged successfully from BackupJobs
```

**For Test Connection:**
```
╔═══════════════════════════════════════════════════════════╗
║  📩 NEW MESSAGE from TestConnection queue
╚═══════════════════════════════════════════════════════════╝
Queue: ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b.test-connection.queue
Message type determined: TestConnection
Processing test connection for {ConnectionId}
Test connection {ConnectionId} completed: SUCCESS
✓ Message acknowledged successfully from TestConnection
```

### 4. Verify in API Logs
```
[10:20:26 INF] Created BackupHistory record {BackupHistoryId} for Job {JobId}
[10:20:48 INF] Backup job {JobId} published to RabbitMQ
[10:21:02 INF] Sent BackupStarted notification to tenant {TenantId}
[10:21:15 INF] BackupHistory {BackupHistoryId} progress updated: 25%
[10:21:30 INF] BackupHistory {BackupHistoryId} progress updated: 50%
[10:21:45 INF] BackupHistory {BackupHistoryId} progress updated: 75%
[10:22:00 INF] BackupHistory {BackupHistoryId} completed successfully
```

### 5. Check Frontend
- Backup should appear in execution history
- Status badge should show "Completed" (green)
- File size and duration displayed
- Download button available

## 🔍 Troubleshooting

### Worker not receiving messages
1. Check RabbitMQ queue exists: `backup.jobs.{tenantId}`
2. Verify worker TenantId matches API TenantId
3. Check RabbitMQ connection in worker logs
4. Verify API is publishing to correct queue

### pg_dump fails
1. Ensure PostgreSQL client tools installed
2. Check database connection string is correct
3. Verify database credentials have backup permissions
4. Check PostgreSQL server is accessible from worker

### Upload to Azure fails
1. Verify Azure Storage connection string is correct
2. Check container exists or worker can create it
3. Verify network access to Azure Storage
4. Check Azure Storage credentials/SAS token validity

### Messages not acknowledged
1. Check worker authorization service
2. Verify worker API key is valid
3. Check worker is registered with correct TenantId
4. Review worker logs for authorization failures

## ✅ Next Steps

1. ✅ Worker consuming backup jobs - COMPLETED
2. ✅ Executing pg_dump - COMPLETED
3. ✅ Compressing files - COMPLETED
4. ✅ Uploading to Azure - COMPLETED
5. ✅ Status reporting - COMPLETED
6. ⏳ Implement MySQL backup strategy
7. ⏳ Implement SQL Server backup strategy
8. ⏳ Add backup file encryption
9. ⏳ Implement backup restoration
10. ⏳ Add backup verification/integrity checks

## 📚 Related Documentation

- [POSTGRESQL_TOOLS.md](./POSTGRESQL_TOOLS.md) - PostgreSQL client installation
- [README.md](./README.md) - Worker setup and configuration
- [Dockerfile](./Dockerfile) - Docker image build instructions
