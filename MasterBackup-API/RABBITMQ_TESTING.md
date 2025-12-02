# RabbitMQ Implementation - Testing Guide

## ✅ Fase 2.1 - RabbitMQ Completada

### Componentes Implementados

1. **Domain Models** (`Domain/Models/`)
   - `DatabaseConnectionInfo.cs` - Información de conexión para Workers
   - `BackupJobMessage.cs` - Mensaje API → Worker para ejecutar backup
   - `BackupJobResult.cs` - Resultado Worker → API después de backup

2. **Interface Actualizada** (`Application/Common/Interfaces/`)
   - `IMessageQueueService.cs` - 6 métodos para RabbitMQ operations
   - `QueueStats.cs` - Estadísticas de colas

3. **Servicio RabbitMQ** (`Infrastructure/Services/`)
   - `RabbitMQService.cs` - Implementación completa con:
     - Exchange tipo Topic: `masterbackup.exchange`
     - Queues por tenant: `backup.jobs.{tenantId}`, `backup.results.{tenantId}`
     - Dead Letter Queue (DLQ): `backup.jobs.{tenantId}.dlq`
     - Connection management con auto-recovery
     - Publisher confirms (eliminado en v7.x)
     - Suscripción a resultados con consumer async

4. **Integración con BackupJob** (`Infrastructure/Jobs/`)
   - `BackupJob.cs` - Actualizado para publicar mensajes a RabbitMQ cuando se dispara un CRON schedule
   - Incluye decryption de password y construcción de connection string

5. **Endpoints de Testing** (`Presentation/Controllers/`)
   - `POST /api/BackupSchedules/test-publish-job` - Publicar backup job de prueba
   - `GET /api/BackupSchedules/queue-stats` - Estadísticas de cola del tenant
   - `GET /api/BackupSchedules/rabbitmq-health` - Health check de RabbitMQ

6. **Configuración**
   - `appsettings.json` - Ya incluye sección RabbitMQ
   - `RabbitMQ.Client 7.2.0` - Instalado

---

## 🐰 Paso 1: Ejecutar RabbitMQ con Docker

```powershell
# Pull y ejecutar RabbitMQ con management plugin
docker run -d --name rabbitmq `
  -p 5672:5672 `
  -p 15672:15672 `
  -e RABBITMQ_DEFAULT_USER=guest `
  -e RABBITMQ_DEFAULT_PASS=guest `
  rabbitmq:3-management

# Verificar que está corriendo
docker ps | Select-String rabbitmq
```

**RabbitMQ Management UI:** http://localhost:15672
- Usuario: `guest`
- Contraseña: `guest`

---

## 🔬 Paso 2: Probar RabbitMQ con Endpoints

### 2.1 Verificar Health Check

```http
GET http://localhost:5000/api/BackupSchedules/rabbitmq-health
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "healthy": true,
  "timestamp": "2025-12-01T..."
}
```

---

### 2.2 Publicar Mensaje de Prueba

```http
POST http://localhost:5000/api/BackupSchedules/test-publish-job
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "message": "Test backup job published successfully",
  "jobId": "123e4567-e89b-12d3-a456-426614174000",
  "tenantId": "789e4567-e89b-12d3-a456-426614174001",
  "queueName": "backup.jobs.789e4567-e89b-12d3-a456-426614174001"
}
```

**Verificar en RabbitMQ UI:**
1. Ir a http://localhost:15672
2. Pestaña **Exchanges** → Ver `masterbackup.exchange` (tipo: topic)
3. Pestaña **Queues** → Ver:
   - `backup.jobs.{tenantId}` (1 mensaje ready)
   - `backup.results.{tenantId}` (0 mensajes)
   - `backup.jobs.{tenantId}.dlq` (0 mensajes)

---

### 2.3 Ver Estadísticas de Cola

```http
GET http://localhost:5000/api/BackupSchedules/queue-stats
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "tenantId": "789e4567-e89b-12d3-a456-426614174001",
  "queueName": "backup.jobs.789e4567-e89b-12d3-a456-426614174001",
  "messageCount": 1,
  "consumerCount": 0
}
```

---

## 📋 Paso 3: Verificar Mensaje Publicado en RabbitMQ UI

1. **Ir a Queues** → Click en `backup.jobs.{tenantId}`
2. **Get Messages** → Click "Get Message(s)"
3. **Verificar el payload JSON:**

```json
{
  "JobId": "123e4567-...",
  "TenantId": "789e4567-...",
  "BackupScheduleId": "456e7890-...",
  "DatabaseConnection": {
    "DatabaseConnectionId": "...",
    "Name": "Test Database",
    "ConnectionString": "Host=localhost;Port=5432;Database=testdb;...",
    "DatabaseType": "PostgreSQL"
  },
  "BlobStorageConnectionString": "UseDevelopmentStorage=true",
  "ContainerName": "backups-789e4567-...",
  "BackupFileName": "test-backup-20251201-143025.backup",
  "TimeoutMinutes": 30,
  "MaxRetries": 3,
  "CurrentRetry": 0,
  "ScheduledTime": "2025-12-01T14:30:25Z",
  "CompressionType": "gzip"
}
```

**Propiedades del mensaje:**
- `persistent`: `true`
- `content_type`: `application/json`
- `message_id`: `{JobId}`
- `headers`:
  - `tenant-id`: `{TenantId}`
  - `backup-schedule-id`: `{BackupScheduleId}`
  - `retry-count`: `0`

---

## 🔄 Paso 4: Probar Integración con Quartz.NET

Cuando un **BackupSchedule** se ejecute por su CRON schedule:

1. **Quartz.NET** dispara `BackupJob.Execute()`
2. **BackupJob**:
   - Lee BackupSchedule y DatabaseConnection de TenantDbContext
   - Desencripta password
   - Construye connection string
   - Crea `BackupJobMessage`
   - Publica mensaje a RabbitMQ queue: `backup.jobs.{tenantId}`
   - Actualiza `LastRun` y `NextRun` en BackupSchedule

3. **RabbitMQ**:
   - Enruta mensaje a la cola del tenant
   - Worker (Fase 7) consumirá el mensaje
   - Worker ejecutará backup y publicará resultado a `backup.results.{tenantId}`

**Para probar:**
- Crear un BackupSchedule con CRON: `0/30 * * * * ?` (cada 30 segundos)
- Esperar a que se dispare
- Ver logs en consola:
  ```
  [INFO] Executing scheduled backup job. BackupScheduleId: ...
  [INFO] Backup job {JobId} published successfully for BackupSchedule {ScheduleId}
  ```
- Verificar mensaje en RabbitMQ UI

---

## 🧪 Casos de Uso Implementados

### ✅ Caso 1: Crear Queue para Tenant Nuevo
```csharp
await _messageQueueService.CreateTenantQueueAsync(tenantId);
```
**Crea:**
- Exchange: `masterbackup.exchange` (si no existe)
- Queue: `backup.jobs.{tenantId}` con DLQ binding
- Queue: `backup.results.{tenantId}`
- Queue: `backup.jobs.{tenantId}.dlq`

### ✅ Caso 2: Publicar Backup Job
```csharp
var message = new BackupJobMessage { ... };
await _messageQueueService.PublishBackupJobAsync(tenantId, message);
```
**Resultado:**
- Mensaje enrutado a `backup.jobs.{tenantId}`
- Routing key: `backup.job.{tenantId}`
- Persistent y con headers

### ✅ Caso 3: Suscribirse a Resultados (Fase 6)
```csharp
await _messageQueueService.SubscribeToBackupResultsAsync(tenantId, async (result) => {
    // Procesar resultado del backup
    if (result.Success) {
        // Actualizar BackupSchedule, crear registro en BackupHistory
    }
});
```

### ✅ Caso 4: Health Check
```csharp
var isHealthy = await _messageQueueService.IsHealthyAsync();
```

### ✅ Caso 5: Estadísticas
```csharp
var stats = await _messageQueueService.GetQueueStatsAsync(tenantId);
Console.WriteLine($"Messages in queue: {stats.MessageCount}");
```

---

## 🚀 Próximos Pasos

### **Fase 2.2: Azure Blob Storage** (Siguiente)
- Instalar `Azure.Storage.Blobs` package
- Crear `IBlobStorageService` interface
- Implementar `AzureBlobStorageService`
- Container por tenant: `backups-{tenantId}`
- Métodos: UploadAsync, DownloadAsync, DeleteAsync, ListAsync, GetSasUrlAsync

### **Fase 2.3: SignalR Real-time Notifications**
- Crear `BackupNotificationHub`
- Implementar `INotificationHubService`
- Enviar notificaciones en tiempo real sobre:
  - Backup iniciado
  - Backup en progreso (%)
  - Backup completado/fallido

### **Fase 7: Worker Implementation**
- Consumir mensajes de `backup.jobs.{tenantId}`
- Ejecutar pg_dump, mysqldump, sqlcmd
- Subir archivo a Azure Blob Storage
- Publicar resultado a `backup.results.{tenantId}`

---

## 📝 Notas Técnicas

### Cambios en RabbitMQ.Client 7.x
- ✅ `ConfirmSelectAsync()` - Eliminado (publisher confirms ahora son automáticos)
- ✅ `WaitForConfirmsOrDieAsync()` - Eliminado
- ✅ `BasicPublishAsync()` - API actualizada
- ✅ Connection/Channel management - Async by default

### Arquitectura de Queues
```
masterbackup.exchange (topic)
    ├─ routing: backup.job.{tenantId}
    │   └─> backup.jobs.{tenantId}
    │       └─ DLQ: backup.jobs.{tenantId}.dlq
    │
    └─ routing: backup.result.{tenantId}
        └─> backup.results.{tenantId}
```

### Seguridad
- ✅ Contraseñas encriptadas con `EncryptionService`
- ✅ Connection strings se construyen con password desencriptado
- ⚠️ No enviar connection strings a logs
- ⚠️ Workers deben validar tenant isolation

---

## ✅ Checklist de Implementación

- [x] Instalar RabbitMQ.Client 7.2.0
- [x] Crear modelos de mensajes (DatabaseConnectionInfo, BackupJobMessage, BackupJobResult)
- [x] Actualizar IMessageQueueService con 6 métodos
- [x] Implementar RabbitMQService completo con exchange, queues, DLQ
- [x] Configurar appsettings.json con RabbitMQ
- [x] Integrar BackupJob con message queue
- [x] Agregar endpoints de testing al controller
- [x] Build exitoso sin errores
- [x] Documentación de testing

---

## 🎯 Estado Actual

**Fase 2.1 - RabbitMQ: 100% Completada ✅**

El sistema está listo para:
1. ✅ Publicar backup jobs a RabbitMQ cuando Quartz.NET dispara schedules
2. ✅ Crear queues automáticamente por tenant
3. ✅ Health checks y estadísticas
4. ✅ Infraestructura para suscribirse a resultados (Fase 6)
5. ⏳ Espera implementación de Workers (Fase 7) para consumir mensajes

**Siguiente:** Fase 2.2 - Azure Blob Storage para almacenar archivos de backup
