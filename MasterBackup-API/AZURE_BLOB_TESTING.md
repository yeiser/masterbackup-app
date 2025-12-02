# Azure Blob Storage Implementation - Testing Guide

## ✅ Fase 2.2 - Azure Blob Storage Completada

### Componentes Implementados

1. **Interface** (`Application/Common/Interfaces/IBlobStorageService.cs`)
   - `UploadBackupAsync()` - Subir archivo de backup
   - `DownloadBackupAsync()` - Descargar archivo de backup
   - `DeleteBackupAsync()` - Eliminar archivo de backup
   - `ListBackupsAsync()` - Listar archivos con filtro opcional
   - `GetBlobSasUrlAsync()` - Generar URL temporal (SAS)
   - `GetContainerStatsAsync()` - Estadísticas de almacenamiento
   - `IsHealthyAsync()` - Health check
   - `EnsureContainerExistsAsync()` - Crear container si no existe

2. **Clases de Resultado**
   - `BlobUploadResult` - Resultado de upload con URL, tamaño, ETag
   - `BackupDownloadResult` - Stream de descarga con metadata
   - `BlobMetadata` - Metadata de archivo (nombre, tamaño, fechas)
   - `ContainerStats` - Estadísticas (count, total size GB/MB)

3. **Servicio Implementado** (`Infrastructure/Services/AzureBlobStorageService.cs`)
   - Container por tenant: `backups-{tenantId}` (lowercase)
   - Metadata automática: `uploaded_at`, `tenant_id`
   - Error handling completo con logging
   - Soporte para SAS URLs
   - Integración con Azure SDK v12.26.0

4. **Endpoints de Testing** (`Presentation/Controllers/BackupSchedulesController.cs`)
   - `POST /api/BackupSchedules/test-upload-blob` - Upload con IFormFile
   - `GET /api/BackupSchedules/list-blobs` - Listar backups
   - `GET /api/BackupSchedules/download-blob/{fileName}` - Descargar archivo
   - `DELETE /api/BackupSchedules/delete-blob/{fileName}` - Eliminar archivo
   - `GET /api/BackupSchedules/get-blob-sas-url/{fileName}` - Generar SAS URL
   - `GET /api/BackupSchedules/blob-storage-stats` - Ver estadísticas
   - `GET /api/BackupSchedules/blob-storage-health` - Health check

5. **Configuración**
   - `appsettings.json` - Sección `AzureStorage` agregada
   - `ConnectionString`: `UseDevelopmentStorage=true` (Azurite)
   - `ContainerPrefix`: `backups`
   - `Program.cs` - Servicio registrado como Singleton

6. **Integración con BackupJob**
   - Conexión string tomada de configuración
   - Container name dinámico basado en tenantId
   - Listo para que Workers suban archivos de backup

---

## 📦 Paso 1: Instalar y Ejecutar Azurite (Azure Storage Emulator)

### Opción 1: Docker (Recomendado)

```powershell
# Pull y ejecutar Azurite emulator
docker run -d --name azurite `
  -p 10000:10000 `
  -p 10001:10001 `
  -p 10002:10002 `
  -v ${PWD}/azurite-data:/data `
  mcr.microsoft.com/azure-storage/azurite

# Verificar que está corriendo
docker ps | Select-String azurite

# Ver logs
docker logs azurite
```

### Opción 2: npm global

```powershell
# Instalar Azurite globalmente
npm install -g azurite

# Ejecutar en background
Start-Process powershell -ArgumentList "azurite --silent --location c:\azurite --debug c:\azurite\debug.log"

# O ejecutar en foreground
azurite
```

**Connection String por defecto:**
```
UseDevelopmentStorage=true
```

**Endpoints:**
- Blob Service: `http://127.0.0.1:10000`
- Queue Service: `http://127.0.0.1:10001`
- Table Service: `http://127.0.0.1:10002`

---

## 🔬 Paso 2: Probar Azure Blob Storage con Endpoints

### 2.1 Verificar Health Check

```http
GET http://localhost:5000/api/BackupSchedules/blob-storage-health
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

### 2.2 Subir Archivo de Prueba

**Usando Postman/Insomnia:**

```http
POST http://localhost:5000/api/BackupSchedules/test-upload-blob
Authorization: Bearer {tu_jwt_token}
Content-Type: multipart/form-data

file: [seleccionar archivo] (ej: test.txt, backup.sql, etc.)
```

**Usando PowerShell:**

```powershell
$token = "tu_jwt_token_aqui"
$filePath = "C:\path\to\test-file.txt"

$headers = @{
    "Authorization" = "Bearer $token"
}

$form = @{
    file = Get-Item -Path $filePath
}

Invoke-RestMethod -Uri "http://localhost:5000/api/BackupSchedules/test-upload-blob" `
    -Method Post `
    -Headers $headers `
    -Form $form
```

**Respuesta Esperada:**
```json
{
  "message": "File uploaded successfully to Azure Blob Storage",
  "blobUrl": "http://127.0.0.1:10000/devstoreaccount1/backups-{tenantId}/test-20251201-143025-file.txt",
  "blobName": "test-20251201-143025-file.txt",
  "sizeBytes": 1024,
  "sizeMB": 0.0009765625,
  "uploadedAt": "2025-12-01T14:30:25.123Z",
  "contentType": "text/plain",
  "eTag": "\"0x8DCBF1234567890\"",
  "containerName": "backups-{tenantId}"
}
```

---

### 2.3 Listar Todos los Archivos

```http
GET http://localhost:5000/api/BackupSchedules/list-blobs
Authorization: Bearer {tu_jwt_token}
```

**Con filtro por prefijo:**
```http
GET http://localhost:5000/api/BackupSchedules/list-blobs?prefix=test-
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "tenantId": "789e4567-e89b-12d3-a456-426614174001",
  "count": 3,
  "totalSizeMB": 2.5,
  "blobs": [
    {
      "name": "test-20251201-143025-file.txt",
      "url": "http://127.0.0.1:10000/devstoreaccount1/backups-{tenantId}/test-20251201-143025-file.txt",
      "sizeBytes": 1024,
      "sizeMB": 0.0009765625,
      "createdAt": "2025-12-01T14:30:25Z",
      "lastModified": "2025-12-01T14:30:25Z",
      "contentType": "text/plain",
      "metadata": {
        "uploaded_at": "2025-12-01T14:30:25.123Z",
        "tenant_id": "789e4567-...",
        "original_name": "file.txt",
        "uploaded_by": "test-endpoint"
      },
      "eTag": "\"0x8DCBF1234567890\""
    }
  ]
}
```

---

### 2.4 Descargar Archivo

```http
GET http://localhost:5000/api/BackupSchedules/download-blob/test-20251201-143025-file.txt
Authorization: Bearer {tu_jwt_token}
```

**Respuesta:**
- HTTP 200 con el archivo como attachment
- Content-Type según el tipo original
- Content-Disposition: `attachment; filename="test-20251201-143025-file.txt"`

**PowerShell:**
```powershell
$token = "tu_jwt_token_aqui"
$fileName = "test-20251201-143025-file.txt"
$outputPath = "C:\Downloads\$fileName"

$headers = @{
    "Authorization" = "Bearer $token"
}

Invoke-WebRequest -Uri "http://localhost:5000/api/BackupSchedules/download-blob/$fileName" `
    -Method Get `
    -Headers $headers `
    -OutFile $outputPath

Write-Host "Archivo descargado: $outputPath"
```

---

### 2.5 Generar SAS URL Temporal

```http
GET http://localhost:5000/api/BackupSchedules/get-blob-sas-url/test-20251201-143025-file.txt?expiryMinutes=30
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "fileName": "test-20251201-143025-file.txt",
  "sasUrl": "http://127.0.0.1:10000/devstoreaccount1/backups-{tenantId}/test-20251201-143025-file.txt?sv=2021-08-06&se=2025-12-01T15%3A00%3A25Z&sr=b&sp=r&sig=ABC123...",
  "expiresInMinutes": 30,
  "expiresAt": "2025-12-01T15:00:25Z"
}
```

**Uso del SAS URL:**
- Puedes acceder directamente desde el navegador sin autenticación
- Válido solo por el tiempo especificado (30 minutos)
- Útil para compartir backups temporalmente

---

### 2.6 Ver Estadísticas de Almacenamiento

```http
GET http://localhost:5000/api/BackupSchedules/blob-storage-stats
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada:**
```json
{
  "tenantId": "789e4567-e89b-12d3-a456-426614174001",
  "containerName": "backups-789e4567-e89b-12d3-a456-426614174001",
  "blobCount": 5,
  "totalSizeBytes": 52428800,
  "totalSizeMB": 50.0,
  "totalSizeGB": 0.048828125
}
```

---

### 2.7 Eliminar Archivo

```http
DELETE http://localhost:5000/api/BackupSchedules/delete-blob/test-20251201-143025-file.txt
Authorization: Bearer {tu_jwt_token}
```

**Respuesta Esperada (exitoso):**
```json
{
  "message": "File test-20251201-143025-file.txt deleted successfully",
  "fileName": "test-20251201-143025-file.txt",
  "tenantId": "789e4567-e89b-12d3-a456-426614174001"
}
```

**Respuesta (no encontrado):**
```json
{
  "error": "File test-20251201-143025-file.txt not found"
}
```

---

## 🔍 Paso 3: Verificar en Azure Storage Explorer

### Opción 1: Azure Storage Explorer (Desktop App)

1. **Descargar**: https://azure.microsoft.com/en-us/products/storage/storage-explorer/
2. **Conectar a Azurite**:
   - Click en "Connect" → "Local Storage Emulator"
   - Default endpoint: `http://127.0.0.1:10000`
3. **Navegar**:
   - Emulator & Attached → Storage Accounts → (Local and Attached) → Blob Containers
   - Ver container: `backups-{tenantId}`
   - Ver archivos subidos, metadata, propiedades

### Opción 2: VS Code Extension

1. **Instalar extensión**: "Azure Storage" por Microsoft
2. **Conectar**:
   - Azure Storage → Attach to Local Emulator
3. **Explorar**:
   - Blob Containers → `backups-{tenantId}`
   - Ver/descargar/eliminar archivos directamente

---

## 🧪 Paso 4: Probar Integración con Worker (Simulación)

### Flujo Completo de Backup

1. **Quartz.NET dispara BackupJob**
2. **BackupJob crea BackupJobMessage** con:
   - `BlobStorageConnectionString` desde configuración
   - `ContainerName`: `backups-{tenantId}`
   - `BackupFileName`: `schedule-name_20251201-143025.backup`

3. **Worker (Fase 7) consumirá el mensaje y**:
   - Ejecutará pg_dump/mysqldump
   - Subirá archivo a Blob Storage usando `IBlobStorageService`
   - Publicará resultado a RabbitMQ

### Simular Upload de Worker

```powershell
# Crear archivo de backup simulado
$backupContent = "-- PostgreSQL database dump`n-- Dumped from database version 14.5`n..."
$backupFile = "C:\temp\test-backup.sql"
[System.IO.File]::WriteAllText($backupFile, $backupContent)

# Subir usando endpoint
$token = "tu_jwt_token_aqui"
$headers = @{ "Authorization" = "Bearer $token" }
$form = @{ file = Get-Item -Path $backupFile }

Invoke-RestMethod -Uri "http://localhost:5000/api/BackupSchedules/test-upload-blob" `
    -Method Post -Headers $headers -Form $form
```

---

## 📊 Casos de Uso Implementados

### ✅ Caso 1: Upload con Metadata
```csharp
var metadata = new Dictionary<string, string>
{
    { "backup_schedule_id", scheduleId.ToString() },
    { "database_name", "production_db" },
    { "database_type", "PostgreSQL" },
    { "compression", "gzip" }
};

await _blobStorageService.UploadBackupAsync(
    tenantId, 
    "backup.sql.gz", 
    fileStream, 
    metadata, 
    "application/gzip");
```

### ✅ Caso 2: Listar Backups Recientes
```csharp
// Listar últimos backups de un schedule
var prefix = "my-schedule_";
var backups = await _blobStorageService.ListBackupsAsync(tenantId, prefix);
var recent = backups.Take(10); // Ordenados por fecha descendente
```

### ✅ Caso 3: Download para Restore
```csharp
var result = await _blobStorageService.DownloadBackupAsync(tenantId, fileName);
using var fileStream = File.Create("restored-backup.sql");
await result.Content.CopyToAsync(fileStream);
```

### ✅ Caso 4: Limpieza Automática (Retention Policy)
```csharp
// Obtener backups antiguos
var allBackups = await _blobStorageService.ListBackupsAsync(tenantId);
var oldBackups = allBackups
    .Where(b => b.CreatedAt < DateTime.UtcNow.AddDays(-schedule.RetentionDays))
    .ToList();

// Eliminar
foreach (var backup in oldBackups)
{
    await _blobStorageService.DeleteBackupAsync(tenantId, backup.Name);
}
```

### ✅ Caso 5: Compartir Backup Temporalmente
```csharp
// Generar URL que expira en 1 hora
var sasUrl = await _blobStorageService.GetBlobSasUrlAsync(
    tenantId, 
    fileName, 
    expiryMinutes: 60);

// Enviar URL por email para descarga
await _emailService.SendBackupLinkAsync(userEmail, sasUrl);
```

---

## 🔐 Seguridad y Mejores Prácticas

### ✅ Implementado
- **Tenant Isolation**: Cada tenant tiene su propio container
- **Lowercase Container Names**: Cumple requisitos de Azure (3-63 chars, lowercase, alphanumeric)
- **Metadata Automática**: `uploaded_at`, `tenant_id` en cada blob
- **Private Containers**: `PublicAccessType.None`
- **SAS URLs**: Acceso temporal controlado con expiración
- **Logging**: Todas las operaciones registradas con tamaños y tenantIds

### ⚠️ Para Producción
- **Connection String**: Cambiar de `UseDevelopmentStorage=true` a real Azure Storage
- **Managed Identity**: Usar Azure Managed Identity en lugar de connection strings
- **Lifecycle Policies**: Configurar en Azure para auto-delete después de RetentionDays
- **Geo-Redundancy**: Usar GRS/RA-GRS para backups críticos
- **Encryption**: Habilitar encryption at rest (Azure lo hace por defecto)
- **Monitoring**: Configurar Azure Monitor para alertas de espacio/costo

---

## 🚀 Próximos Pasos

### **Fase 2.3: SignalR Real-time Notifications** (Siguiente)
- Crear `BackupNotificationHub`
- DTOs: `BackupNotificationDto`, `BackupProgressDto`
- Implementar `INotificationHubService`
- Frontend: Conectar con `@microsoft/signalr`
- Notificaciones en tiempo real:
  - Backup iniciado
  - Progreso de backup (%)
  - Backup completado/fallido
  - Backup disponible para descarga

### **Fase 6: Instant Backups & BackupHistory**
- Endpoint: `POST /api/BackupSchedules/{id}/execute-now`
- Crear registro en `BackupHistory` al completar
- Almacenar resultado del Worker (success/failure)
- Link al blob en Azure Storage

### **Fase 7: Worker Implementation**
- Consumir mensajes de RabbitMQ
- Ejecutar pg_dump, mysqldump, sqlcmd
- Subir archivo a Blob Storage
- Publicar resultado a `backup.results.{tenantId}`

---

## 📝 Configuración para Producción

### Azure Storage Account

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=masterbackupprod;AccountKey=...;EndpointSuffix=core.windows.net",
    "ContainerPrefix": "backups"
  }
}
```

### Environment Variables (Recomendado)

```bash
AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
```

```csharp
// Program.cs
builder.Configuration["AzureStorage:ConnectionString"] = 
    Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING") 
    ?? builder.Configuration["AzureStorage:ConnectionString"];
```

---

## ✅ Checklist de Implementación

- [x] Instalar Azure.Storage.Blobs 12.26.0
- [x] Crear IBlobStorageService con 8 métodos
- [x] Implementar AzureBlobStorageService completo
- [x] Configurar appsettings.json con AzureStorage
- [x] Registrar servicio como Singleton en DI
- [x] Crear clases de resultado (BlobUploadResult, BackupDownloadResult, etc.)
- [x] Agregar 7 endpoints de testing al controller
- [x] Integrar con BackupJob (connection string y container name)
- [x] Build exitoso sin errores
- [x] Documentación de testing

---

## 🎯 Estado Actual

**Fase 2.2 - Azure Blob Storage: 100% Completada ✅**

El sistema está listo para:
1. ✅ Subir archivos de backup a Azure Blob Storage
2. ✅ Container aislado por tenant (`backups-{tenantId}`)
3. ✅ Descargar, listar, eliminar backups
4. ✅ Generar SAS URLs para acceso temporal
5. ✅ Ver estadísticas de almacenamiento por tenant
6. ✅ Health checks de conectividad
7. ✅ BackupJob configurado con blob storage
8. ⏳ Espera Workers (Fase 7) para uploads automáticos

**Siguiente:** Fase 2.3 - SignalR para notificaciones en tiempo real
