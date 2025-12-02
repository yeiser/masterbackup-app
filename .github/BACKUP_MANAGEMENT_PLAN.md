# Plan de Implementación - Fase: Gestión de Backups

## Visión General

Implementar el sistema completo de gestión de backups automatizados con arquitectura multi-tenant, incluyendo programación flexible, ejecución distribuida mediante workers, almacenamiento en Azure y notificaciones en tiempo real.

---

## 📋 Fase 1: Fundamentos y Entidades (Semana 1)

### 1.1 Diseño de Base de Datos

**Backend - Entidades en Master Database:**

```csharp
// Master DB
- Workers (Id, TenantId, Name, ApiKey, IpAddress, Status, LastHeartbeat, IsActive)
- SubscriptionPlans (Id, Name, MaxDatabases, MaxBackupsPerMonth, StorageGB, Price)
- TenantSubscriptions (Id, TenantId, SubscriptionPlanId, StartDate, EndDate, IsActive)
```

**Backend - Entidades en Tenant Database:**

```csharp
// Tenant DB
- DatabaseConnections (Id, Name, Type, Host, Port, Database, Username, EncryptedPassword, IsActive)
- BackupSchedules (Id, DatabaseConnectionId, CronExpression, RetentionDays, IsActive, NextRun)
- BackupExecutions (Id, BackupScheduleId, WorkerId, Status, StartTime, EndTime, FileSize, ErrorMessage)
- BackupFiles (Id, BackupExecutionId, FileName, BlobUrl, FileSize, CreatedAt, ExpiresAt)
- UserNotificationPreferences (Id, UserId, EmailOnSuccess, EmailOnFailure, EmailOnWarning)
```

**Tareas:**
- [ ] Crear entidades en `Domain/Entities/`
- [ ] Configurar en `MasterDbContext` y `TenantDbContext`
- [ ] Crear enums: `DatabaseType`, `BackupStatus`, `WorkerStatus`
- [ ] Crear migraciones para ambos contextos
- [ ] Aplicar migraciones

---

## 📋 Fase 2: Infraestructura de Comunicación (Semana 1-2)

### 2.1 Configuración de RabbitMQ

**Backend:**
```csharp
// Application/Common/Interfaces/
- IMessageQueueService.cs
  - PublishBackupJob(tenantId, backupJobDto)
  - CreateTenantQueue(tenantId)
  - DeleteTenantQueue(tenantId)

// Infrastructure/Services/
- RabbitMQService.cs (implementación)

// appsettings.json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

**Tareas:**
- [ ] Instalar RabbitMQ.Client NuGet package
- [ ] Crear `IMessageQueueService` interface
- [ ] Implementar `RabbitMQService`
- [ ] Registrar en DI en `Program.cs`
- [ ] Crear middleware para auto-crear queue al registrar tenant
- [ ] Testing de conexión y envío de mensajes

### 2.2 Configuración de Azure Blob Storage

**Backend:**
```csharp
// Application/Common/Interfaces/
- IBlobStorageService.cs
  - UploadAsync(tenantId, stream, fileName)
  - DownloadAsync(tenantId, blobUrl)
  - DeleteAsync(tenantId, blobUrl)
  - GetSasUrl(tenantId, blobUrl, expiryMinutes)

// Infrastructure/Services/
- AzureBlobStorageService.cs

// appsettings.json
{
  "AzureBlobStorage": {
    "ConnectionString": "...",
    "ContainerPrefix": "backups-"
  }
}
```

**Tareas:**
- [ ] Instalar Azure.Storage.Blobs NuGet package
- [ ] Crear `IBlobStorageService` interface
- [ ] Implementar `AzureBlobStorageService`
- [ ] Container por tenant: `backups-{tenantId}`
- [ ] Registrar en DI
- [ ] Testing de upload/download/delete

### 2.3 Configuración de SignalR

**Backend:**
```csharp
// Infrastructure/Hubs/
- BackupNotificationHub.cs
  - Groups por TenantId
  - Métodos: NotifyBackupStarted, NotifyBackupCompleted, NotifyBackupFailed

// Program.cs
builder.Services.AddSignalR();
app.MapHub<BackupNotificationHub>("/hubs/backup-notifications");
```

**Frontend:**
```typescript
// src/app/core/services/
- signalr.service.ts
  - connect()
  - onBackupStarted(callback)
  - onBackupCompleted(callback)
  - onBackupFailed(callback)
  - disconnect()
```

**Tareas:**
- [ ] Instalar Microsoft.AspNetCore.SignalR (backend)
- [ ] Crear `BackupNotificationHub`
- [ ] Registrar SignalR en `Program.cs`
- [ ] Instalar @microsoft/signalr (frontend)
- [ ] Crear `SignalRService` en Angular
- [ ] Testing de conexión y notificaciones

---

## 📋 Fase 3: Scheduling con Quartz.NET (Semana 2)

### 3.1 Configuración de Quartz.NET

**Backend:**
```csharp
// Infrastructure/Jobs/
- BackupJob.cs : IJob
  - Execute() → Enviar mensaje a RabbitMQ

// Infrastructure/Services/
- IBackupSchedulerService.cs
  - ScheduleBackup(backupSchedule)
  - UnscheduleBackup(backupScheduleId)
  - PauseBackup(backupScheduleId)
  - ResumeBackup(backupScheduleId)
  - GetNextFireTime(backupScheduleId)

- BackupSchedulerService.cs (implementación con Quartz)

// Program.cs
builder.Services.AddQuartz(q => {
    q.UseMicrosoftDependencyInjectionJobFactory();
});
builder.Services.AddQuartzHostedService(opt => {
    opt.WaitForJobsToComplete = true;
});
```

**Tareas:**
- [ ] Instalar Quartz NuGet package
- [ ] Crear `BackupJob` que envíe mensaje a RabbitMQ
- [ ] Crear `IBackupSchedulerService` interface
- [ ] Implementar `BackupSchedulerService`
- [ ] Registrar Quartz en `Program.cs`
- [ ] Testing de jobs programados

### 3.2 Validación de Expresiones CRON

**Backend:**
```csharp
// Application/Common/Validators/
- CronExpressionValidator : AbstractValidator<string>
  - Validar formato CRON
  - Validar que no sea en el pasado
  - Calcular próximas 5 ejecuciones para preview
```

**Frontend:**
```typescript
// src/app/shared/components/
- cron-builder.component.ts
  - Interfaz amigable: Diario, Semanal, Mensual, Personalizado
  - Preview de próximas 5 ejecuciones
  - Validación en tiempo real
```

**Tareas:**
- [ ] Crear validador de CRON en backend
- [ ] Crear componente `CronBuilderComponent` en frontend
- [ ] Integrar con formulario de BackupSchedule
- [ ] Testing con diferentes expresiones CRON

---

## 📋 Fase 4: CRUD de Conexiones de BD (Semana 2-3)

### 4.1 Backend - Commands y Queries

**Estructura:**
```
Application/Features/DatabaseConnections/
├── Commands/
│   ├── CreateDatabaseConnectionCommand.cs
│   ├── CreateDatabaseConnectionCommandHandler.cs
│   ├── UpdateDatabaseConnectionCommand.cs
│   ├── UpdateDatabaseConnectionCommandHandler.cs
│   ├── DeleteDatabaseConnectionCommand.cs
│   ├── DeleteDatabaseConnectionCommandHandler.cs
│   ├── TestDatabaseConnectionCommand.cs
│   └── TestDatabaseConnectionCommandHandler.cs
└── Queries/
    ├── GetDatabaseConnectionsQuery.cs
    ├── GetDatabaseConnectionsQueryHandler.cs
    ├── GetDatabaseConnectionByIdQuery.cs
    └── GetDatabaseConnectionByIdQueryHandler.cs
```

**DTOs:**
```csharp
// Application/Common/DTOs/
- DatabaseConnectionDto.cs
- CreateDatabaseConnectionDto.cs
- UpdateDatabaseConnectionDto.cs
- TestConnectionResultDto.cs
```

**Validators:**
```csharp
// Application/Common/Validators/
- CreateDatabaseConnectionDtoValidator.cs
- UpdateDatabaseConnectionDtoValidator.cs
```

**Controller:**
```csharp
// Presentation/Controllers/
- DatabaseConnectionsController.cs
  - GET /api/database-connections (List)
  - GET /api/database-connections/{id}
  - POST /api/database-connections (Create)
  - PUT /api/database-connections/{id} (Update)
  - DELETE /api/database-connections/{id}
  - POST /api/database-connections/test (Test connection)
```

**Tareas:**
- [ ] Crear todos los commands y handlers
- [ ] Crear queries y handlers
- [ ] Crear DTOs y validators
- [ ] Implementar encriptación de contraseñas (AES)
- [ ] Crear `DatabaseConnectionsController`
- [ ] Testing de todos los endpoints

### 4.2 Frontend - Componentes

**Estructura:**
```
src/app/features/database-connections/
├── components/
│   ├── database-connections-list/
│   │   ├── database-connections-list.component.ts
│   │   ├── database-connections-list.component.html
│   │   └── database-connections-list.component.css
│   ├── database-connection-form/
│   │   ├── database-connection-form.component.ts
│   │   ├── database-connection-form.component.html
│   │   └── database-connection-form.component.css
│   └── test-connection-modal/
│       ├── test-connection-modal.component.ts
│       ├── test-connection-modal.component.html
│       └── test-connection-modal.component.css
└── services/
    └── database-connection.service.ts
```

**Características UI:**
- Lista con DataTables (búsqueda, paginación, ordenamiento)
- Formulario modal para crear/editar
- Botón "Probar Conexión" con feedback visual
- Estados: Activo/Inactivo con toggle
- Confirmación antes de eliminar
- Indicadores de tipo de BD (PostgreSQL, MySQL, SQL Server)

**Tareas:**
- [ ] Crear `DatabaseConnectionService`
- [ ] Crear componente de lista con tabla Metronic
- [ ] Crear formulario modal con validaciones
- [ ] Implementar test de conexión con modal de progreso
- [ ] Agregar ruta `/database-connections`
- [ ] Agregar item en sidebar
- [ ] Testing E2E del CRUD completo

---

## 📋 Fase 5: Gestión de Backups Programados (Semana 3-4)

### 5.1 Backend - Commands y Queries

**Estructura:**
```
Application/Features/BackupSchedules/
├── Commands/
│   ├── CreateBackupScheduleCommand.cs
│   ├── CreateBackupScheduleCommandHandler.cs
│   ├── UpdateBackupScheduleCommand.cs
│   ├── UpdateBackupScheduleCommandHandler.cs
│   ├── DeleteBackupScheduleCommand.cs
│   ├── DeleteBackupScheduleCommandHandler.cs
│   ├── PauseBackupScheduleCommand.cs
│   ├── PauseBackupScheduleCommandHandler.cs
│   ├── ResumeBackupScheduleCommand.cs
│   └── ResumeBackupScheduleCommandHandler.cs
└── Queries/
    ├── GetBackupSchedulesQuery.cs
    ├── GetBackupSchedulesQueryHandler.cs
    ├── GetBackupScheduleByIdQuery.cs
    ├── GetBackupScheduleByIdQueryHandler.cs
    ├── PreviewCronExecutionsQuery.cs
    └── PreviewCronExecutionsQueryHandler.cs
```

**DTOs:**
```csharp
- BackupScheduleDto.cs
- CreateBackupScheduleDto.cs
- UpdateBackupScheduleDto.cs
- CronExecutionPreviewDto.cs
```

**Controller:**
```csharp
- BackupSchedulesController.cs
  - GET /api/backup-schedules
  - GET /api/backup-schedules/{id}
  - POST /api/backup-schedules
  - PUT /api/backup-schedules/{id}
  - DELETE /api/backup-schedules/{id}
  - POST /api/backup-schedules/{id}/pause
  - POST /api/backup-schedules/{id}/resume
  - POST /api/backup-schedules/preview-cron
```

**Lógica en Handler de Create:**
1. Validar DatabaseConnection existe y está activo
2. Validar expresión CRON
3. Crear BackupSchedule en TenantDb
4. Registrar job en Quartz.NET con `BackupSchedulerService`
5. Calcular y guardar NextRun

**Tareas:**
- [ ] Crear commands, queries y handlers
- [ ] Crear DTOs y validators
- [ ] Integrar con `BackupSchedulerService`
- [ ] Crear `BackupSchedulesController`
- [ ] Testing de programación y desprogramación

### 5.2 Frontend - Componentes

**Estructura:**
```
src/app/features/backup-schedules/
├── components/
│   ├── backup-schedules-list/
│   ├── backup-schedule-form/
│   └── cron-preview-modal/
└── services/
    └── backup-schedule.service.ts
```

**Características UI:**
- Lista con estado visual: Activo/Pausado/Error
- Mostrar próxima ejecución (countdown)
- Formulario con `CronBuilderComponent`
- Preview de próximas 5 ejecuciones antes de guardar
- Acciones: Pausar, Reanudar, Editar, Eliminar
- Filtros: Por DatabaseConnection, Estado

**Tareas:**
- [ ] Crear `BackupScheduleService`
- [ ] Crear componentes de lista y formulario
- [ ] Integrar `CronBuilderComponent`
- [ ] Mostrar countdown para próxima ejecución
- [ ] Agregar ruta `/backup-schedules`
- [ ] Testing E2E

---

## 📋 Fase 6: Ejecución de Backups Instantáneos (Semana 4)

### 6.1 Backend - Instant Backup

**Commands:**
```csharp
Application/Features/Backups/
├── Commands/
│   ├── ExecuteInstantBackupCommand.cs
│   └── ExecuteInstantBackupCommandHandler.cs
```

**Handler Logic:**
1. Validar DatabaseConnection existe y está activo
2. Crear BackupExecution con Status=Pending
3. Enviar mensaje a RabbitMQ `{tenantId}.backup.queue`:
```json
{
  "BackupExecutionId": "guid",
  "DatabaseConnectionId": "guid",
  "Type": "PostgreSQL",
  "Host": "...",
  "Port": 5432,
  "Database": "...",
  "Username": "...",
  "Password": "...",
  "TenantId": "guid",
  "IsInstant": true
}
```
4. Retornar BackupExecutionId al frontend

**Controller:**
```csharp
- BackupsController.cs
  - POST /api/backups/execute-instant
```

**Tareas:**
- [ ] Crear command y handler
- [ ] Integrar con `IMessageQueueService`
- [ ] Crear `BackupsController`
- [ ] Testing de envío a RabbitMQ

### 6.2 Frontend - Instant Backup

**Estructura Actual Implementada:**
```typescript
src/app/features/backup-execution/
├── components/
│   └── backup-execution-list/
│       ├── backup-execution-list.component.ts (599 líneas)
│       ├── backup-execution-list.component.html
│       ├── backup-execution-list.component.css
│       └── backup-execution-list.component.spec.ts
├── models/
│   └── backup-history.models.ts
└── services/
    └── backup-history.service.ts
```

**Características Implementadas:**
- ✅ Lista completa de historial de backups con paginación
- ✅ Vista de estadísticas con métricas y gráficos
- ✅ Filtros avanzados (BD, programación, estado, fechas, tipo)
- ✅ Acciones individuales: Ver detalles, Reintentar, Descargar, Eliminar
- ✅ Operaciones en lote: Selección múltiple, Eliminación masiva
- ✅ Integración completa con BackupHistoryController (8 endpoints)
- ✅ SweetAlert2 para confirmaciones y modales de detalles
- ✅ UI Metronic con animaciones y efectos hover
- ✅ Badges de estado con iconos FontAwesome
- ✅ Formateo de fechas, tamaños y duraciones
- ✅ KTMenu para menús de acciones por registro
- ✅ Reinicialización automática de menús en actualizaciones

**Pendiente para Completar Fase 6.2:**
- [ ] Crear modal de "Instant Backup" para ejecutar backup manual
  - Componente: `instant-backup-modal.component.ts`
  - Ubicación: `src/app/features/backup-execution/components/instant-backup-modal/`
  - Funcionalidad:
    - Modal con SweetAlert2 o modal Metronic
    - Selector de DatabaseConnection
    - Opciones: Tipo de compresión, timeout, reintentos
    - Botón "Ejecutar Ahora"
    - Indicador de progreso mientras se encola

- [ ] Agregar botón "Ejecutar Backup Instantáneo" en:
  - Lista de DatabaseConnections (botón por conexión)
  - Header de BackupExecutionListComponent
  - DatabaseConnection details view

- [ ] Integrar con ExecuteInstantBackupCommand del backend
  - Endpoint: POST `/api/backups/execute-instant`
  - Payload: `{ databaseConnectionId, compressionType?, timeoutMinutes?, maxRetries? }`

- [ ] Implementar monitoreo en tiempo real con SignalR
  - Conectar a BackupNotificationHub
  - Escuchar eventos: BackupStarted, BackupProgress, BackupCompleted, BackupFailed
  - Actualizar lista automáticamente al recibir notificaciones
  - Mostrar toast notifications para eventos importantes

- [ ] Agregar indicador visual de backups en progreso
  - Badge "En Progreso" con spinner animado
  - Progress bar si hay % de completado
  - Tiempo transcurrido en tiempo real

- [ ] Testing E2E del flujo completo:
  - Abrir modal → Seleccionar BD → Ejecutar → Monitorear → Ver resultado

**Mejoras Sugeridas:**
- [ ] Botón "Ejecutar Ahora" en cada backup schedule de la lista
- [ ] Quick action panel en dashboard con "Backup Instantáneo"
- [ ] Historial de últimos 5 backups instantáneos en sidebar
- [ ] Notificaciones desktop con Notification API del navegador

---

## 📋 Fase 7: Worker Dockerizado (Semana 4-5)

### 7.1 Desarrollo del Worker

**Estructura del Proyecto:**
```
MasterBackup-Worker/
├── Dockerfile
├── Program.cs
├── Services/
│   ├── RabbitMQConsumer.cs
│   ├── BackupExecutor.cs
│   ├── PostgreSQLBackupStrategy.cs
│   ├── AzureBlobUploader.cs
│   └── ApiClient.cs
├── Models/
│   ├── BackupJobMessage.cs
│   └── BackupResult.cs
└── appsettings.json
```

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Instalar PostgreSQL client tools
RUN apt-get update && \
    apt-get install -y postgresql-client && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MasterBackup-Worker.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MasterBackup-Worker.dll"]
```

**Worker Logic:**
1. Conectar a RabbitMQ y escuchar `{tenantId}.backup.queue`
2. Recibir mensaje de backup job
3. Actualizar BackupExecution Status=InProgress via API
4. Ejecutar `pg_dump` según estrategia
5. Comprimir archivo con gzip
6. Subir a Azure Blob Storage
7. Actualizar BackupExecution con resultado via API
8. Enviar notificación por email (si configurado)
9. Notificar a API para SignalR
10. ACK mensaje de RabbitMQ

**Tareas:**
- [ ] Crear proyecto Worker en .NET 8
- [ ] Implementar `RabbitMQConsumer`
- [ ] Implementar `BackupExecutor` con Strategy Pattern
- [ ] Implementar `PostgreSQLBackupStrategy` con pg_dump
- [ ] Implementar `AzureBlobUploader`
- [ ] Implementar `ApiClient` para actualizar estado
- [ ] Crear Dockerfile con PostgreSQL tools
- [ ] Testing local con Docker Compose
- [ ] Testing de ejecución completa

### 7.2 Backend - Worker Management

**Entities:**
```csharp
// Master DB
public class Worker
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string ApiKey { get; set; }
    public string IpAddress { get; set; }
    public WorkerStatus Status { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public bool IsActive { get; set; }
}

public enum WorkerStatus
{
    Online,
    Offline,
    Busy,
    Error
}
```

**Commands:**
```csharp
Application/Features/Workers/
├── Commands/
│   ├── RegisterWorkerCommand.cs
│   ├── UpdateWorkerHeartbeatCommand.cs
│   └── DeactivateWorkerCommand.cs
└── Queries/
    ├── GetWorkersQuery.cs
    └── GetWorkerByIdQuery.cs
```

**Controller:**
```csharp
- WorkersController.cs
  - POST /api/workers/register (Worker se auto-registra)
  - POST /api/workers/heartbeat (Worker envía heartbeat cada 30s)
  - GET /api/workers (Admin lista workers)
  - DELETE /api/workers/{id} (Admin desactiva worker)
```

**Tareas:**
- [ ] Crear entidad Worker en Master DB
- [ ] Crear commands, queries y handlers
- [ ] Crear `WorkersController`
- [ ] Implementar heartbeat en Worker
- [ ] Background service para marcar workers offline si no hay heartbeat
- [ ] Testing de registro y heartbeat

---

## 📋 Fase 8: Gestión de Archivos de Backup (Semana 5)

### 8.1 Backend - Backup Files

**Queries:**
```csharp
Application/Features/BackupFiles/
├── Queries/
│   ├── GetBackupFilesQuery.cs (con paginación, filtros)
│   ├── GetBackupFilesQueryHandler.cs
│   ├── GetBackupFileByIdQuery.cs
│   └── GetBackupFileByIdQueryHandler.cs
└── Commands/
    ├── DeleteBackupFileCommand.cs
    ├── DeleteBackupFileCommandHandler.cs
    ├── GenerateDownloadUrlCommand.cs
    └── GenerateDownloadUrlCommandHandler.cs
```

**Controller:**
```csharp
- BackupFilesController.cs
  - GET /api/backup-files (List con filtros)
  - GET /api/backup-files/{id}
  - DELETE /api/backup-files/{id}
  - POST /api/backup-files/{id}/download-url (Genera SAS URL)
```

**Background Job:**
```csharp
// Cleanup job para eliminar archivos expirados
Infrastructure/Jobs/
└── BackupFileCleanupJob.cs
    - Ejecutar diario
    - Eliminar BackupFiles donde ExpiresAt < Now
    - Eliminar del Blob Storage
```

**Tareas:**
- [ ] Crear queries y handlers
- [ ] Crear command para delete y SAS URL
- [ ] Crear `BackupFilesController`
- [ ] Implementar cleanup job con Quartz
- [ ] Testing de listado, descarga y eliminación

### 8.2 Frontend - Backup Files

**Componentes:**
```typescript
src/app/features/backup-files/
├── components/
│   ├── backup-files-list/
│   ├── backup-file-details/
│   └── download-progress-modal/
└── services/
    └── backup-file.service.ts
```

**Características UI:**
- Lista con DataTables avanzada
- Filtros: Por DatabaseConnection, Rango de fechas, Estado
- Columnas: Nombre, Tamaño, Fecha, Estado, Acciones
- Botón de descarga con progreso
- Confirmación antes de eliminar
- Indicador de expiración (días restantes)
- Preview de metadata del backup

**Tareas:**
- [ ] Crear `BackupFileService`
- [ ] Crear componente de lista con filtros
- [ ] Implementar descarga con progress bar
- [ ] Modal de confirmación para delete
- [ ] Agregar ruta `/backup-files`
- [ ] Testing E2E

---

## 📋 Fase 9: Notificaciones y Preferencias (Semana 6)

### 9.1 Backend - Notification Preferences

**Entities:**
```csharp
// Tenant DB
public class UserNotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EmailOnSuccess { get; set; }
    public bool EmailOnFailure { get; set; }
    public bool EmailOnWarning { get; set; }
}
```

**Commands:**
```csharp
Application/Features/NotificationPreferences/
├── Commands/
│   ├── UpdateNotificationPreferencesCommand.cs
│   └── UpdateNotificationPreferencesCommandHandler.cs
└── Queries/
    ├── GetNotificationPreferencesQuery.cs
    └── GetNotificationPreferencesQueryHandler.cs
```

**Service:**
```csharp
Infrastructure/Services/
└── INotificationService.cs
    - SendBackupSuccessNotification(tenantId, backupExecution)
    - SendBackupFailureNotification(tenantId, backupExecution)
    - SendBackupWarningNotification(tenantId, backupExecution, warning)
```

**Email Templates:**
```
Infrastructure/EmailTemplates/
├── BackupSuccessTemplate.html
├── BackupFailureTemplate.html
└── BackupWarningTemplate.html
```

**Tareas:**
- [ ] Crear entidad UserNotificationPreference
- [ ] Crear commands, queries y handlers
- [ ] Implementar `NotificationService`
- [ ] Crear templates HTML para emails
- [ ] Integrar en Worker para enviar notificaciones
- [ ] Testing de envío de emails

### 9.2 Frontend - Notification Settings

**Componente:**
```typescript
src/app/features/settings/
└── components/
    └── notification-preferences/
```

**Características:**
- Form con toggles para cada tipo de notificación
- Preview de emails
- Guardar preferencias por usuario

**Tareas:**
- [ ] Crear componente de preferencias
- [ ] Integrar en página de Settings
- [ ] Testing de guardado

---

## 📋 Fase 10: Dashboard y Estadísticas (Semana 6)

### 10.1 Backend - Dashboard Queries

**Queries:**
```csharp
Application/Features/Dashboard/
└── Queries/
    ├── GetDashboardStatsQuery.cs
    ├── GetDashboardStatsQueryHandler.cs
    ├── GetRecentBackupsQuery.cs
    ├── GetRecentBackupsQueryHandler.cs
    ├── GetBackupTrendsQuery.cs
    └── GetBackupTrendsQueryHandler.cs
```

**DTOs:**
```csharp
- DashboardStatsDto.cs
  - TotalBackups
  - SuccessfulBackups
  - FailedBackups
  - TotalStorageUsed
  - ActiveSchedules
  - ActiveWorkers
  
- BackupTrendDto.cs
  - Date
  - SuccessCount
  - FailureCount
  - AverageSize
```

**Controller:**
```csharp
- DashboardController.cs
  - GET /api/dashboard/stats
  - GET /api/dashboard/recent-backups
  - GET /api/dashboard/trends?days=30
```

**Tareas:**
- [ ] Crear queries y handlers
- [ ] Crear DTOs
- [ ] Crear `DashboardController`
- [ ] Testing de queries con datos de prueba

### 10.2 Frontend - Dashboard Mejorado

**Componente:**
```typescript
src/app/features/dashboard/
└── components/
    ├── dashboard-stats/
    ├── recent-backups-table/
    ├── backup-trends-chart/
    └── quick-actions-panel/
```

**Características:**
- 4 cards de estadísticas principales
- Gráfico de tendencias (Chart.js)
- Tabla de backups recientes
- Panel de acciones rápidas
- Indicador de workers activos
- Auto-refresh cada 30 segundos
- SignalR para actualizaciones en tiempo real

**Tareas:**
- [ ] Crear `DashboardService`
- [ ] Implementar componentes de stats
- [ ] Integrar Chart.js para gráficos
- [ ] Conectar SignalR para updates
- [ ] Testing de dashboard completo

---

## 📋 Fase 11: Testing y Optimización (Semana 7)

### 11.1 Testing Backend

**Unit Tests:**
- [ ] Handlers de Commands y Queries
- [ ] Validators de FluentValidation
- [ ] Services (EmailService, NotificationService, etc.)
- [ ] Strategy Pattern para backups

**Integration Tests:**
- [ ] Controllers con base de datos en memoria
- [ ] Flujo completo de registro de tenant
- [ ] Flujo completo de backup programado
- [ ] Flujo completo de backup instantáneo

**Tareas:**
- [ ] Configurar xUnit y Moq
- [ ] Crear tests para handlers críticos
- [ ] Crear tests de integración para flujos principales
- [ ] Code coverage mínimo 70%

### 11.2 Testing Frontend

**Unit Tests:**
- [ ] Services (AuthService, BackupService, etc.)
- [ ] Componentes críticos
- [ ] Helpers y utilities

**E2E Tests:**
- [ ] Flujo de registro y login
- [ ] Crear DatabaseConnection
- [ ] Crear BackupSchedule
- [ ] Ejecutar backup instantáneo
- [ ] Ver y descargar backup

**Tareas:**
- [ ] Configurar Jasmine/Karma
- [ ] Crear tests unitarios
- [ ] Configurar Cypress para E2E
- [ ] Tests E2E de flujos críticos

### 11.3 Optimización

**Backend:**
- [ ] Indexar tablas críticas (BackupExecutions, BackupFiles)
- [ ] Implementar caché con Redis para queries frecuentes
- [ ] Optimizar queries con Include y Select
- [ ] Implementar rate limiting en endpoints públicos

**Frontend:**
- [ ] Lazy loading de módulos
- [ ] Implementar virtual scrolling en tablas grandes
- [ ] Optimizar bundle size
- [ ] Implementar service worker para PWA

**RabbitMQ:**
- [ ] Configurar prefetch count
- [ ] Dead letter queue para mensajes fallidos
- [ ] Monitoreo de queues

**Azure Blob:**
- [ ] Lifecycle policies para auto-delete
- [ ] Usar cool storage para backups antiguos

---

## 📋 Fase 12: Documentación y Deployment (Semana 7-8)

### 12.1 Documentación

**Backend:**
- [ ] Swagger/OpenAPI completo con ejemplos
- [ ] README con instrucciones de instalación
- [ ] ARCHITECTURE.md detallando flujos
- [ ] API_REFERENCE.md con todos los endpoints

**Frontend:**
- [ ] Compodoc para documentación de código
- [ ] USER_GUIDE.md para usuarios finales
- [ ] DEVELOPER_GUIDE.md para desarrolladores

**Worker:**
- [ ] README con instrucciones de Docker
- [ ] DEPLOYMENT.md para instalación en cliente

**Tareas:**
- [ ] Generar documentación Swagger
- [ ] Escribir guías de usuario
- [ ] Crear diagramas de arquitectura
- [ ] Documentar variables de entorno

### 12.2 Docker Compose

**Archivo docker-compose.yml:**
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: masterbackup_master
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  rabbitmq:
    image: rabbitmq:3-management
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"

  api:
    build:
      context: ./MasterBackup-API
      dockerfile: Dockerfile
    environment:
      MASTER_DATABASE_CONNECTION: "Host=postgres;Port=5432;Database=masterbackup_master;Username=postgres;Password=password"
      RABBITMQ__HOST: rabbitmq
      AZURE_STORAGE_CONNECTION_STRING: "${AZURE_STORAGE_CONNECTION}"
    ports:
      - "7001:8080"
    depends_on:
      - postgres
      - rabbitmq

  frontend:
    build:
      context: ./MasterBackup-App
      dockerfile: Dockerfile
    environment:
      API_URL: http://api:8080
    ports:
      - "80:80"
    depends_on:
      - api

  worker:
    build:
      context: ./MasterBackup-Worker
      dockerfile: Dockerfile
    environment:
      RABBITMQ__HOST: rabbitmq
      API_URL: http://api:8080
      AZURE_STORAGE_CONNECTION_STRING: "${AZURE_STORAGE_CONNECTION}"
      TENANT_ID: "${TENANT_ID}"
      WORKER_API_KEY: "${WORKER_API_KEY}"
    depends_on:
      - rabbitmq
      - api

volumes:
  postgres_data:
```

**Tareas:**
- [ ] Crear Dockerfile para API
- [ ] Crear Dockerfile para Frontend (Nginx)
- [ ] Crear docker-compose.yml completo
- [ ] Testing de despliegue local
- [ ] Crear scripts de inicialización

### 12.3 CI/CD Pipeline

**GitHub Actions:**
```yaml
# .github/workflows/deploy.yml
- Build y test Backend
- Build y test Frontend
- Build Worker Docker image
- Push a Docker Hub / Azure Container Registry
- Deploy a Azure App Service / Kubernetes
```

**Tareas:**
- [ ] Configurar GitHub Actions
- [ ] Automatizar build y tests
- [ ] Automatizar deployment
- [ ] Configurar secrets en GitHub

---

## 📋 Fase 13: Funcionalidades Adicionales (Futuro)

### 13.1 Gestión de Subscription Plans

**Entities:**
```csharp
- SubscriptionPlan (Name, MaxDatabases, MaxBackupsPerMonth, StorageGB, Price)
- TenantSubscription (TenantId, SubscriptionPlanId, StartDate, EndDate)
```

**Features:**
- CRUD de planes de suscripción
- Asignar plan a tenant
- Validar límites en operaciones
- Billing y facturación

### 13.2 Reportes y Analytics

**Features:**
- Exportar reportes PDF/Excel
- Gráficos de tendencias avanzados
- Alertas proactivas
- Compliance reports

### 13.3 Restauración de Backups

**Features:**
- Subir archivo de backup
- Validar integridad
- Ejecutar pg_restore en worker
- Monitoreo de progreso

### 13.4 Multi-Database Support

**Features:**
- MySQL backup strategy
- SQL Server backup strategy
- MongoDB backup strategy

---

## 🎯 Métricas de Éxito

### Funcionales:
- ✅ CRUD completo de DatabaseConnections
- ✅ Programación flexible con CRON
- ✅ Ejecución de backups (programados e instantáneos)
- ✅ Workers funcionando con Docker
- ✅ Almacenamiento en Azure Blob Storage
- ✅ Notificaciones por email
- ✅ Notificaciones en tiempo real con SignalR
- ✅ Dashboard con estadísticas

### No Funcionales:
- ✅ API response time < 200ms (p95)
- ✅ Worker procesa backup en < 5 min para DB de 1GB
- ✅ Uptime 99.9%
- ✅ Soportar 100 tenants concurrentes
- ✅ Code coverage > 70%
- ✅ Zero data loss

---

## 📅 Timeline Estimado

| Fase | Duración | Semanas |
|------|----------|---------|
| 1. Fundamentos y Entidades | 3-4 días | Semana 1 |
| 2. Infraestructura (RabbitMQ, Azure, SignalR) | 4-5 días | Semana 1-2 |
| 3. Quartz.NET Scheduling | 2-3 días | Semana 2 |
| 4. CRUD DatabaseConnections | 3-4 días | Semana 2-3 |
| 5. Backup Schedules | 4-5 días | Semana 3-4 |
| 6. Instant Backups | 2-3 días | Semana 4 |
| 7. Worker Dockerizado | 5-6 días | Semana 4-5 |
| 8. Backup Files Management | 3-4 días | Semana 5 |
| 9. Notificaciones | 3-4 días | Semana 6 |
| 10. Dashboard y Stats | 3-4 días | Semana 6 |
| 11. Testing y Optimización | 5-6 días | Semana 7 |
| 12. Documentación y Deployment | 5-6 días | Semana 7-8 |

**Total Estimado: 6-8 semanas**

---

## 🚀 Próximos Pasos Inmediatos

1. **Revisar y aprobar este plan**
2. **Configurar repositorio para Worker** (nuevo proyecto)
3. **Crear cuenta de Azure Blob Storage**
4. **Instalar RabbitMQ** (Docker local para desarrollo)
5. **Comenzar Fase 1**: Crear entidades y migraciones

---

¿Deseas que comencemos con la **Fase 1** o necesitas ajustes al plan? 🎯
