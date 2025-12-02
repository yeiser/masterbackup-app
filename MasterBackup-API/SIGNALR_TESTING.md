# SignalR Real-Time Notifications - Testing Guide

## Overview

This guide covers testing the SignalR implementation for real-time backup notifications in MasterBackup-API.

## Architecture

- **Hub Endpoint**: `/hubs/backup-notifications`
- **Hub Class**: `BackupNotificationHub` in `Infrastructure/Hubs/`
- **Service Interface**: `INotificationService` in `Application/Common/Interfaces/`
- **Service Implementation**: `NotificationService` in `Infrastructure/Services/`
- **DTOs**: `BackupNotificationDtos.cs` in `Application/Common/DTOs/`

## Notification Types

### 1. BackupStartedDto
Sent when a backup job begins execution.

```json
{
  "notificationType": "Started",
  "jobId": "guid",
  "tenantId": "guid",
  "backupScheduleId": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "databaseType": "PostgreSQL",
  "estimatedDurationMinutes": 5
}
```

### 2. BackupProgressDto
Sent periodically during backup execution to show progress.

```json
{
  "notificationType": "Progress",
  "jobId": "guid",
  "tenantId": "guid",
  "backupScheduleId": "guid",
  "timestamp": "2024-01-15T10:32:00Z",
  "progressPercentage": 50,
  "currentStep": "Dumping database tables",
  "processedBytes": 5242880,
  "totalBytes": 10485760,
  "elapsedTime": "00:02:30",
  "estimatedTimeRemaining": "00:02:30"
}
```

### 3. BackupCompletedDto
Sent when a backup completes successfully.

```json
{
  "notificationType": "Completed",
  "jobId": "guid",
  "tenantId": "guid",
  "backupScheduleId": "guid",
  "timestamp": "2024-01-15T10:35:00Z",
  "success": true,
  "blobUrl": "https://storage.blob.core.windows.net/backups-tenant/backup.sql.gz",
  "blobName": "backup.sql.gz",
  "backupSizeBytes": 10485760,
  "backupSizeMB": 10.0,
  "backupSizeGB": 0.01,
  "duration": "00:05:00",
  "startTime": "2024-01-15T10:30:00Z",
  "endTime": "2024-01-15T10:35:00Z",
  "compressionType": "GZIP",
  "metadata": {
    "database_name": "mydb",
    "table_count": "25"
  }
}
```

### 4. BackupFailedDto
Sent when a backup fails.

```json
{
  "notificationType": "Failed",
  "jobId": "guid",
  "tenantId": "guid",
  "backupScheduleId": "guid",
  "timestamp": "2024-01-15T10:32:00Z",
  "errorMessage": "Connection to database timed out",
  "errorCode": "DB_TIMEOUT",
  "stackTrace": "at System.Data.SqlClient.SqlConnection.Open()...",
  "retryCount": 1,
  "maxRetries": 3,
  "willRetry": true,
  "nextRetryAt": "2024-01-15T10:37:00Z",
  "duration": "00:02:00"
}
```

### 5. BackupEventDto
Generic event for custom notifications.

```json
{
  "eventType": "CustomEvent",
  "jobId": "guid",
  "tenantId": "guid",
  "backupScheduleId": "guid",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "message": "Custom event message",
    "severity": "info"
  }
}
```

## Hub Features

### Tenant-Based Grouping
When a client connects, they are automatically added to their tenant group: `tenant_{tenantId}`

### Schedule-Based Grouping
Clients can subscribe to specific backup schedules using the hub method:
```javascript
await connection.invoke("SubscribeToSchedule", scheduleId);
await connection.invoke("UnsubscribeFromSchedule", scheduleId);
```

### Connection Info
Get information about the current connection:
```javascript
const info = await connection.invoke("GetConnectionInfo");
// Returns: { connectionId, userId, tenantId }
```

## Frontend Integration

### 1. Install SignalR Client Library

```bash
npm install @microsoft/signalr
```

### 2. Connect to Hub

```typescript
import * as signalR from '@microsoft/signalr';

// Get JWT token from your auth service
const token = getAuthToken();

// Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://localhost:5001/hubs/backup-notifications', {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Handle connection events
connection.onreconnecting((error) => {
  console.log('Connection lost, reconnecting...', error);
});

connection.onreconnected((connectionId) => {
  console.log('Reconnected with connectionId:', connectionId);
});

connection.onclose((error) => {
  console.log('Connection closed', error);
});

// Start connection
try {
  await connection.start();
  console.log('SignalR Connected');
} catch (err) {
  console.error('SignalR Connection Error:', err);
}
```

### 3. Subscribe to Notifications

```typescript
// Backup started
connection.on('BackupStarted', (notification) => {
  console.log('Backup started:', notification);
  // Update UI: show backup as "In Progress"
  updateBackupStatus(notification.backupScheduleId, 'running');
});

// Backup progress
connection.on('BackupProgress', (notification) => {
  console.log('Backup progress:', notification);
  // Update UI: show progress bar
  updateProgressBar(notification.backupScheduleId, notification.progressPercentage);
  updateProgressDetails(notification.currentStep, notification.elapsedTime);
});

// Backup completed
connection.on('BackupCompleted', (notification) => {
  console.log('Backup completed:', notification);
  // Update UI: show backup as "Success"
  updateBackupStatus(notification.backupScheduleId, 'completed');
  showSuccessMessage(`Backup completed: ${notification.backupSizeMB.toFixed(2)} MB`);
});

// Backup failed
connection.on('BackupFailed', (notification) => {
  console.log('Backup failed:', notification);
  // Update UI: show backup as "Failed"
  updateBackupStatus(notification.backupScheduleId, 'failed');
  showErrorMessage(`Backup failed: ${notification.errorMessage}`);
  
  if (notification.willRetry) {
    showRetryInfo(`Will retry at ${notification.nextRetryAt}`);
  }
});

// Custom events
connection.on('BackupEvent', (notification) => {
  console.log('Backup event:', notification);
  // Handle custom events
  handleCustomEvent(notification.eventType, notification.data);
});
```

### 4. Subscribe to Specific Schedules

```typescript
// Subscribe to a specific backup schedule
async function subscribeToSchedule(scheduleId: string) {
  try {
    await connection.invoke('SubscribeToSchedule', scheduleId);
    console.log(`Subscribed to schedule: ${scheduleId}`);
  } catch (err) {
    console.error('Failed to subscribe:', err);
  }
}

// Unsubscribe from a schedule
async function unsubscribeFromSchedule(scheduleId: string) {
  try {
    await connection.invoke('UnsubscribeFromSchedule', scheduleId);
    console.log(`Unsubscribed from schedule: ${scheduleId}`);
  } catch (err) {
    console.error('Failed to unsubscribe:', err);
  }
}
```

### 5. Complete Angular Example

```typescript
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';

export interface BackupNotification {
  notificationType: string;
  jobId: string;
  tenantId: string;
  backupScheduleId: string;
  timestamp: Date;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  private notificationsSubject = new BehaviorSubject<BackupNotification | null>(null);
  
  public notifications$: Observable<BackupNotification | null> = 
    this.notificationsSubject.asObservable();

  constructor(private authService: AuthService) {}

  public async startConnection(): Promise<void> {
    const token = this.authService.getToken();

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/backup-notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Register event handlers
    this.registerHandlers();

    try {
      await this.hubConnection.start();
      console.log('SignalR connection established');
    } catch (err) {
      console.error('SignalR connection error:', err);
      setTimeout(() => this.startConnection(), 5000); // Retry after 5s
    }
  }

  private registerHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('BackupStarted', (notification: BackupNotification) => {
      this.notificationsSubject.next(notification);
    });

    this.hubConnection.on('BackupProgress', (notification: BackupNotification) => {
      this.notificationsSubject.next(notification);
    });

    this.hubConnection.on('BackupCompleted', (notification: BackupNotification) => {
      this.notificationsSubject.next(notification);
    });

    this.hubConnection.on('BackupFailed', (notification: BackupNotification) => {
      this.notificationsSubject.next(notification);
    });

    this.hubConnection.on('BackupEvent', (notification: BackupNotification) => {
      this.notificationsSubject.next(notification);
    });
  }

  public async subscribeToSchedule(scheduleId: string): Promise<void> {
    if (!this.hubConnection) return;
    
    try {
      await this.hubConnection.invoke('SubscribeToSchedule', scheduleId);
    } catch (err) {
      console.error('Failed to subscribe to schedule:', err);
    }
  }

  public async unsubscribeFromSchedule(scheduleId: string): Promise<void> {
    if (!this.hubConnection) return;
    
    try {
      await this.hubConnection.invoke('UnsubscribeFromSchedule', scheduleId);
    } catch (err) {
      console.error('Failed to unsubscribe from schedule:', err);
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
    }
  }
}
```

## API Testing Endpoints

### 1. Test SignalR Notification (All Types)

Send test notifications to all connected clients in the tenant.

**Endpoint**: `POST /api/BackupSchedules/test-signalr-notification`

**Headers**:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Body (Optional)**:
```json
{
  "scheduleId": "guid-optional",
  "notificationType": "Progress"
}
```

**NotificationType values**: `Started`, `Progress`, `Completed`, `Failed`, `Event`

**Example with cURL**:
```bash
# Test "Started" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Started"}'

# Test "Progress" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Progress"}'

# Test "Completed" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Completed"}'

# Test "Failed" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Failed"}'

# Test "Event" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Event"}'
```

### 2. Test Schedule-Specific Notification

Send test notifications to clients subscribed to a specific schedule.

**Endpoint**: `POST /api/BackupSchedules/test-signalr-schedule-notification`

**Headers**:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Body**:
```json
{
  "scheduleId": "your-backup-schedule-guid",
  "notificationType": "Progress"
}
```

**Example with cURL**:
```bash
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-schedule-notification" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "scheduleId": "123e4567-e89b-12d3-a456-426614174000",
    "notificationType": "Progress"
  }'
```

### 3. Check SignalR Health

Verify SignalR hub is available.

**Endpoint**: `GET /api/BackupSchedules/signalr-health`

**Headers**:
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Example with cURL**:
```bash
curl -X GET "https://localhost:5001/api/BackupSchedules/signalr-health" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response**:
```json
{
  "healthy": true,
  "hubEndpoint": "/hubs/backup-notifications",
  "timestamp": "2024-01-15T10:30:00Z",
  "message": "SignalR hub is available. Connect clients to /hubs/backup-notifications"
}
```

## Testing Workflow

### Step 1: Start the API
```bash
cd MasterBackup-API
dotnet run
```

### Step 2: Create a Simple HTML Test Client

Create `test-signalr.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>SignalR Test Client</title>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <h1>SignalR Test Client</h1>
    <div>
        <label>JWT Token:</label><br>
        <textarea id="token" rows="3" cols="80"></textarea><br>
        <button onclick="connect()">Connect</button>
        <button onclick="disconnect()">Disconnect</button>
    </div>
    <div>
        <h3>Subscribe to Schedule:</h3>
        <input type="text" id="scheduleId" placeholder="Schedule GUID">
        <button onclick="subscribe()">Subscribe</button>
        <button onclick="unsubscribe()">Unsubscribe</button>
    </div>
    <div>
        <h3>Connection Status:</h3>
        <div id="status">Disconnected</div>
    </div>
    <div>
        <h3>Notifications:</h3>
        <div id="notifications" style="border: 1px solid #ccc; padding: 10px; height: 400px; overflow-y: scroll;">
        </div>
    </div>

    <script>
        let connection = null;

        async function connect() {
            const token = document.getElementById('token').value;
            
            connection = new signalR.HubConnectionBuilder()
                .withUrl('https://localhost:5001/hubs/backup-notifications', {
                    accessTokenFactory: () => token
                })
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();

            connection.on('BackupStarted', (notification) => {
                addNotification('BackupStarted', notification);
            });

            connection.on('BackupProgress', (notification) => {
                addNotification('BackupProgress', notification);
            });

            connection.on('BackupCompleted', (notification) => {
                addNotification('BackupCompleted', notification);
            });

            connection.on('BackupFailed', (notification) => {
                addNotification('BackupFailed', notification);
            });

            connection.on('BackupEvent', (notification) => {
                addNotification('BackupEvent', notification);
            });

            try {
                await connection.start();
                document.getElementById('status').innerHTML = 'Connected: ' + connection.connectionId;
            } catch (err) {
                document.getElementById('status').innerHTML = 'Connection Error: ' + err;
            }
        }

        async function disconnect() {
            if (connection) {
                await connection.stop();
                document.getElementById('status').innerHTML = 'Disconnected';
            }
        }

        async function subscribe() {
            const scheduleId = document.getElementById('scheduleId').value;
            if (connection && scheduleId) {
                await connection.invoke('SubscribeToSchedule', scheduleId);
                addNotification('System', { message: 'Subscribed to schedule: ' + scheduleId });
            }
        }

        async function unsubscribe() {
            const scheduleId = document.getElementById('scheduleId').value;
            if (connection && scheduleId) {
                await connection.invoke('UnsubscribeFromSchedule', scheduleId);
                addNotification('System', { message: 'Unsubscribed from schedule: ' + scheduleId });
            }
        }

        function addNotification(type, data) {
            const div = document.getElementById('notifications');
            const time = new Date().toLocaleTimeString();
            div.innerHTML += `<div style="margin-bottom: 10px; border-bottom: 1px solid #eee; padding-bottom: 5px;">
                <strong>[${time}] ${type}:</strong><br>
                <pre>${JSON.stringify(data, null, 2)}</pre>
            </div>`;
            div.scrollTop = div.scrollHeight;
        }
    </script>
</body>
</html>
```

### Step 3: Test Complete Flow

1. Open `test-signalr.html` in your browser
2. Get a JWT token by logging in via `/api/Auth/login`
3. Paste the JWT token in the textarea and click "Connect"
4. Use Postman/cURL to send test notifications
5. Observe real-time notifications appearing in the browser

**Test Sequence**:
```bash
# 1. Send "Started" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Started"}'

# 2. Send "Progress" notification (50%)
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Progress"}'

# 3. Send "Completed" notification
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Completed"}'

# Test failed scenario
curl -X POST "https://localhost:5001/api/BackupSchedules/test-signalr-notification" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notificationType": "Failed"}'
```

## Troubleshooting

### Issue: "Connection failed"
**Solution**: 
- Verify API is running on the correct port
- Check CORS settings in `Program.cs`
- Ensure JWT token is valid and not expired

### Issue: "401 Unauthorized"
**Solution**:
- Check JWT token is being sent correctly in `accessTokenFactory`
- Verify token contains `tenant_id` and `user_id` claims
- Confirm `[Authorize]` attribute is not blocking SignalR

### Issue: Not receiving notifications
**Solution**:
- Verify connection is established (check `connection.connectionId`)
- Check browser console for JavaScript errors
- Ensure you're in the correct tenant group
- For schedule-specific notifications, verify you called `SubscribeToSchedule`

### Issue: "Hub not found"
**Solution**:
- Verify hub is mapped in `Program.cs`: `app.MapHub<BackupNotificationHub>("/hubs/backup-notifications")`
- Check endpoint URL matches exactly (case-sensitive)

### Issue: Reconnection issues
**Solution**:
- Use `.withAutomaticReconnect()` when building the connection
- Implement `onreconnected` handler to resubscribe to schedules
- Consider implementing exponential backoff for retries

## Integration with Worker

When implementing the Worker service (Fase 7), it should:

1. **On backup start**: Publish `BackupJobMessage` to RabbitMQ
2. **During backup**: Periodically call `NotifyBackupProgressAsync` via HTTP API
3. **On completion**: Call `NotifyBackupCompletedAsync` with blob URL and size
4. **On failure**: Call `NotifyBackupFailedAsync` with error details

**Worker Integration Example**:
```csharp
// In Worker's backup execution method
await _httpClient.PostAsJsonAsync($"{_apiUrl}/api/Notifications/backup-progress", new
{
    TenantId = tenantId,
    BackupScheduleId = scheduleId,
    ProgressPercentage = 50,
    CurrentStep = "Dumping database",
    ProcessedBytes = bytesProcessed,
    TotalBytes = totalBytes
});
```

## Performance Considerations

- SignalR connections are **persistent WebSocket connections**
- Each connected client consumes server memory
- For production, consider **scaling out** with Azure SignalR Service or Redis backplane
- Implement **connection throttling** if needed
- Monitor **connection count** and **message frequency**

## Security Notes

- Hub uses JWT authentication - claims are extracted automatically
- Tenant isolation via groups (`tenant_{tenantId}`)
- Only authenticated users can connect
- Users can only receive notifications for their own tenant
- Schedule subscriptions don't bypass tenant security

## Next Steps

After testing SignalR:
1. ✅ Implement Fase 6: Instant Backups & BackupHistory
2. ✅ Implement Fase 7: Worker service with real backup execution
3. ✅ Integrate all three infrastructure components (RabbitMQ + Blob Storage + SignalR)
4. ✅ Build frontend UI to display real-time backup status

---

**End of SignalR Testing Guide**
