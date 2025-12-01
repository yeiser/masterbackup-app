# Flujo de Prueba de Conexión de Base de Datos

Este documento describe el flujo completo de prueba de conexión implementado con comunicación asíncrona entre API, RabbitMQ y Worker.

## 🔄 Arquitectura del Flujo

```
┌──────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   Frontend   │─────>│     API      │─────>│   RabbitMQ   │─────>│    Worker    │
│   (Angular)  │      │  (.NET API)  │      │   (Message   │      │  (Consumer)  │
│              │      │              │      │    Queue)    │      │              │
└──────────────┘      └──────────────┘      └──────────────┘      └──────────────┘
                             ↑                                            │
                             │                                            │
                             └────────────────────────────────────────────┘
                                      HTTP POST /api/test-connections/complete
```

## 📋 Flujo Paso a Paso

### 1. Usuario Solicita Prueba de Conexión (Frontend)

**Archivo**: `MasterBackup-App/src/app/features/databases/components/databases.component.ts`

```typescript
testConnection(connectionId: string): void {
  this.databaseService.testConnection(connectionId).subscribe({
    next: () => {
      console.info('Probando conexión... El resultado llegará en breve.');
      this.showInfoAlert('Probando conexión... El resultado llegará en breve.');
    }
  });
}
```

**Request**:
```
POST /api/database-connections/{id}/test
Headers: Authorization: Bearer <jwt_token>
```

**Response Inmediata**:
```json
{
  "message": "Test connection job sent to worker. Result will arrive via SignalR."
}
```

---

### 2. API Valida y Publica a RabbitMQ

**Archivo**: `MasterBackup-API/Application/Features/DatabaseConnections/Commands/TestDatabaseConnectionCommandHandler.cs`

**Pasos**:

1. **Obtener DatabaseConnection desde TenantDbContext**
   ```csharp
   var connection = await _tenantContext.DatabaseConnections
       .FirstOrDefaultAsync(x => x.Id == request.ConnectionId);
   ```

2. **Validar Worker Assignment**
   
   **Modo Dedicado** (`AssignmentMode = Dedicated`):
   - Verificar que `AssignedWorkerId` no sea null
   - Verificar que el worker existe y está activo
   - Verificar que el worker está Online

   **Modo Automático** (`AssignmentMode = Auto`):
   - No requiere worker específico
   - Cualquier worker con tags compatibles puede procesar

3. **Desencriptar Password**
   ```csharp
   var decryptedPassword = _encryptionService.Decrypt(connection.EncryptedPassword);
   ```

4. **Crear TestConnectionMessage**
   ```csharp
   var message = new TestConnectionMessage
   {
       ConnectionId = connection.Id,
       TenantId = tenantId,
       Type = connection.Type,
       Host = connection.Host,
       Port = connection.Port,
       Database = connection.Database,
       Username = connection.Username,
       Password = decryptedPassword,
       SSLMode = connection.SSLMode,
       AssignmentMode = connection.AssignmentMode.ToString(),
       AssignedWorkerId = connection.AssignedWorkerId,
       Tags = connection.Tags
   };
   ```

5. **Publicar en RabbitMQ**
   ```csharp
   await _messageQueueService.PublishTestConnectionJob(tenantId, message);
   ```

**Queue Name**: `{tenantId}.test-connection.queue`

---

### 3. RabbitMQ Almacena el Mensaje

**Configuración** (`appsettings.json`):
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

**Propiedades del Mensaje**:
- **Queue**: `{tenantId}.test-connection.queue`
- **Durable**: `true` (persiste en disco)
- **Persistent**: `true` (mensaje sobrevive reinicio de RabbitMQ)
- **AutoDelete**: `false` (queue no se elimina automáticamente)

---

### 4. Worker Consume el Mensaje

**Archivo**: `MasterBackup-Worker/Infrastructure/MessageQueue/RabbitMQConsumerService.cs`

**Pasos**:

1. **Recibir Mensaje desde RabbitMQ**
   ```csharp
   consumer.Received += async (model, ea) =>
   {
       var body = ea.Body.ToArray();
       var messageJson = Encoding.UTF8.GetString(body);
   ```

2. **Deserializar TestConnectionMessage**
   ```csharp
   var message = JsonSerializer.Deserialize<TestConnectionMessage>(messageJson);
   ```

3. **AUTORIZACIÓN: Validar si el Worker puede procesar**
   
   **Archivo**: `MasterBackup-Worker/Application/Services/WorkerAuthorizationService.cs`

   **Validaciones**:
   - ✅ **TenantId Match**: `message.TenantId == workerConfig.TenantId`
   - ✅ **Modo Dedicado**: `message.AssignedWorkerId == workerConfig.WorkerId`
   - ✅ **Modo Automático**: `message.Tags.Intersect(workerConfig.Tags).Any()`

   ```csharp
   var isAuthorized = await _authorizationService.CanProcessTestConnectionAsync(message);
   
   if (!isAuthorized)
   {
       // NACK con requeue=true (otro worker puede procesarlo)
       _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
       return;
   }
   ```

4. **Ejecutar Prueba de Conexión**
   
   **Archivo**: `MasterBackup-Worker/Application/Services/ConnectionTestService.cs`

   **PostgreSQL**:
   ```csharp
   await using var connection = new NpgsqlConnection(connectionString);
   await connection.OpenAsync();
   
   await using var command = new NpgsqlCommand("SELECT version()", connection);
   var version = await command.ExecuteScalarAsync();
   
   await connection.CloseAsync();
   
   return (true, $"✓ Connection successful! PostgreSQL version: {version}");
   ```

   **MySQL**: *Pendiente de implementar*
   
   **SQL Server**: *Pendiente de implementar*

5. **Notificar API del Resultado**
   
   **Archivo**: `MasterBackup-Worker/Infrastructure/Http/ApiClient.cs`

   ```csharp
   var request = new
   {
       ConnectionId = connectionId,
       Success = success,
       Message = message,
       ServerVersion = ExtractServerVersion(message)
   };
   
   await _httpClient.PostAsJsonAsync("/api/test-connections/complete", request);
   ```

6. **ACK el Mensaje en RabbitMQ**
   ```csharp
   _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
   ```

---

### 5. API Recibe Resultado del Worker

**Archivo**: `MasterBackup-API/Presentation/Controllers/TestConnectionsController.cs`

**Endpoint**: `POST /api/test-connections/complete`

**Request Body**:
```json
{
  "connectionId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "message": "✓ Connection successful! PostgreSQL version: PostgreSQL 15.3",
  "serverVersion": "15.3"
}
```

**Headers**:
```
X-API-Key: <worker_api_key>
```

**Pasos**:

1. **Validar API Key del Worker**
   ```csharp
   var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
   if (string.IsNullOrEmpty(apiKey))
       return Unauthorized();
   ```

2. **Actualizar DatabaseConnection**
   
   **Archivo**: `MasterBackup-API/Application/Features/DatabaseConnections/Commands/UpdateTestResultCommandHandler.cs`

   ```csharp
   connection.LastTestedAt = DateTime.UtcNow;
   connection.LastTestSuccessful = request.Success;
   connection.LastTestStatus = request.Status;
   
   if (request.Success && !string.IsNullOrEmpty(request.ServerVersion))
   {
       connection.EngineVersion = request.ServerVersion;
   }
   
   await _tenantContext.SaveChangesAsync();
   ```

3. **Retornar Respuesta al Worker**
   ```json
   {
     "message": "Test result updated successfully"
   }
   ```

---

### 6. (Futuro) Notificar Frontend vía SignalR

**Pendiente de Implementar**:

```csharp
// En UpdateTestResultCommandHandler
await _hubContext.Clients
    .Group(tenantId.ToString())
    .SendAsync("TestConnectionCompleted", new
    {
        ConnectionId = connection.Id,
        Success = connection.LastTestSuccessful,
        Message = connection.LastTestStatus,
        TestedAt = connection.LastTestedAt
    });
```

**Frontend**:
```typescript
this.signalRService.on('TestConnectionCompleted', (data) => {
  const connection = this.connections.find(c => c.id === data.connectionId);
  if (connection) {
    connection.lastTestedAt = data.testedAt;
    connection.lastTestSuccessful = data.success;
    connection.lastTestStatus = data.message;
    
    this.showAlert(data.success ? 'success' : 'error', data.message);
  }
});
```

---

## 📊 Diagrama de Secuencia

```
Usuario        Frontend       API            RabbitMQ        Worker         Database
  |              |             |                |              |               |
  |─────────────>|             |                |              |               |
  |  Click Test  |             |                |              |               |
  |              |────────────>|                |              |               |
  |              | POST /test  |                |              |               |
  |              |             |───────────────>|              |               |
  |              |             | Publish Msg    |              |               |
  |              |<────────────|                |              |               |
  |              | 202 Accepted|                |              |               |
  |<─────────────|             |                |              |               |
  | "Processing…"|             |                |              |               |
  |              |             |                |<─────────────|               |
  |              |             |                | Consume Msg  |               |
  |              |             |                |              |──────────────>|
  |              |             |                |              | Test Connection
  |              |             |                |              |<──────────────|
  |              |             |                |              |   Result      |
  |              |             |<───────────────────────────────|               |
  |              |             | POST /complete |              |               |
  |              |             |───────────────>|              |               |
  |              |             | Update DB      |              |               |
  |              |             |<───────────────|              |               |
  |              |             |    200 OK      |              |               |
  |              |<────────────|                |              |               |
  |              | (SignalR)   |                |              |               |
  |<─────────────|             |                |              |               |
  | "✓ Success!" |             |                |              |               |
```

---

## 🛠️ Archivos Creados/Modificados

### API (.NET)

#### Nuevos Archivos
- `Presentation/Controllers/TestConnectionsController.cs`
  - Endpoint `POST /api/test-connections/complete`
  - Recibe resultados de Workers

#### Archivos Existentes (Sin Cambios)
- `Application/Features/DatabaseConnections/Commands/TestDatabaseConnectionCommandHandler.cs`
  - Ya estaba publicando a RabbitMQ
- `Infrastructure/Services/RabbitMQService.cs`
  - Ya implementado
- `Application/Features/DatabaseConnections/Commands/UpdateTestResultCommandHandler.cs`
  - Ya implementado

### Worker (.NET)

#### Archivos Modificados
- `Infrastructure/Http/ApiClient.cs`
  - ✅ Implementación completa de `NotifyTestConnectionCompletedAsync`
  - ✅ HttpClient con headers `X-API-Key`
  - ✅ Extracción de versión del servidor del mensaje

- `Infrastructure/MessageQueue/RabbitMQConsumerService.cs`
  - ✅ Cambiado queue name de `{tenantId}.backup.queue` a `{tenantId}.test-connection.queue`
  - ✅ Flujo de autorización → test → notificación → ACK

---

## ⚙️ Configuración Requerida

### API (`appsettings.json`)

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

### Worker (`appsettings.json`)

```json
{
  "Worker": {
    "WorkerId": "550e8400-e29b-41d4-a716-446655440000",
    "TenantId": "123e4567-e89b-12d3-a456-426614174000",
    "WorkerName": "Worker-01",
    "ApiKey": "REPLACE_WITH_WORKER_API_KEY",
    "Tags": ["postgresql", "high-priority"],
    "SupportedDatabaseTypes": ["PostgreSQL"],
    "MaxConcurrentJobs": 3
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  },
  "Api": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

---

## 🚀 Cómo Ejecutar

### 1. Iniciar RabbitMQ

**Docker**:
```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

**Acceso Web**: http://localhost:15672 (guest/guest)

### 2. Iniciar API

```bash
cd MasterBackup-API
dotnet run
```

### 3. Iniciar Worker

```bash
cd MasterBackup-Worker
dotnet run
```

**Output Esperado**:
```
============================================
🚀 MasterBackup Worker Started
============================================
Worker ID: 550e8400-e29b-41d4-a716-446655440000
Tenant ID: 123e4567-e89b-12d3-a456-426614174000
Worker Name: Worker-01
Tags: postgresql, high-priority
Supported Databases: PostgreSQL
Max Concurrent Jobs: 3
============================================
info: RabbitMQConsumerService[0]
      RabbitMQ consumer initialized for queue: 123e4567-e89b-12d3-a456-426614174000.test-connection.queue
```

### 4. Iniciar Frontend

```bash
cd MasterBackup-App
ng serve -o
```

### 5. Probar Conexión

1. Navegar a **Databases**
2. Crear o seleccionar una conexión existente
3. Click en **"Test Connection"**
4. Observar logs en Worker:
   ```
   info: Received message from queue: {...}
   info: Testing connection to PostgreSQL database: localhost:5432/mydb
   info: Successfully connected to PostgreSQL: PostgreSQL 15.3...
   info: Notifying API of test connection result...
   info: Message acknowledged successfully
   ```
5. Verificar actualización en Base de Datos:
   ```sql
   SELECT 
       "Id", 
       "Name", 
       "LastTestedAt", 
       "LastTestSuccessful", 
       "LastTestStatus", 
       "EngineVersion"
   FROM "DatabaseConnections"
   WHERE "Id" = '550e8400-e29b-41d4-a716-446655440000';
   ```

---

## 🐛 Troubleshooting

### Worker no recibe mensajes

**Causas**:
- RabbitMQ no está corriendo
- Queue name incorrecto
- TenantId no coincide

**Solución**:
```bash
# Verificar RabbitMQ
docker ps | grep rabbitmq

# Verificar queues en RabbitMQ Management
# http://localhost:15672/#/queues

# Verificar logs del Worker
dotnet run --configuration Debug
```

### Worker rechaza mensajes (NACK)

**Causas**:
- Worker no autorizado (TenantId, WorkerId, Tags)
- Modo Dedicado pero WorkerId no coincide

**Solución**:
```json
// Verificar configuración en appsettings.json
{
  "Worker": {
    "WorkerId": "DEBE_COINCIDIR_CON_AssignedWorkerId",
    "TenantId": "DEBE_COINCIDIR_CON_TenantId_DB",
    "Tags": ["DEBE_TENER_AL_MENOS_UN_TAG_EN_COMÚN"]
  }
}
```

### API no recibe callback

**Causas**:
- URL incorrecta en Worker config
- API Key inválido
- Firewall bloqueando puerto

**Solución**:
```json
// Worker appsettings.json
{
  "Api": {
    "BaseUrl": "http://localhost:5000"  // Verificar puerto correcto
  }
}
```

---

## ✅ Verificación de Implementación

### Checklist

- [x] API publica mensaje a RabbitMQ
- [x] Worker consume mensaje desde RabbitMQ
- [x] Worker valida autorización antes de procesar
- [x] Worker ejecuta test de conexión (PostgreSQL)
- [x] Worker notifica resultado a API vía HTTP
- [x] API actualiza DatabaseConnection en base de datos
- [x] Ambos proyectos compilan sin errores
- [ ] Frontend recibe notificación vía SignalR (Pendiente)

---

## 📝 Próximos Pasos

1. **Implementar SignalR en API**
   - Crear Hub para notificaciones en tiempo real
   - Enviar evento `TestConnectionCompleted` al frontend

2. **Implementar MySQL y SQL Server en Worker**
   - `ConnectionTestService.TestMySQLConnectionAsync()`
   - `ConnectionTestService.TestSQLServerConnectionAsync()`

3. **Implementar Autenticación de Workers**
   - Tabla `Workers` con API Key
   - Middleware para validar `X-API-Key` en callbacks

4. **Implementar Retry Policy**
   - Reintentos automáticos en caso de fallo
   - Dead Letter Queue para mensajes fallidos

5. **Implementar Heartbeat**
   - Worker envía heartbeat cada 30 segundos
   - API actualiza `Worker.LastHeartbeat` y `Worker.Status`

---

## 📚 Referencias

- [RabbitMQ .NET Client Documentation](https://www.rabbitmq.com/dotnet-api-guide.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [Clean Architecture Pattern](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
