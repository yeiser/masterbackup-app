# BackupHistory Controller Documentation

## Overview
El `BackupHistoryController` proporciona endpoints para gestionar el historial de backups, incluyendo consultas, estadísticas, reintentos y eliminación de registros.

## Endpoints

### 1. GET /api/backuphistory
**Descripción**: Obtiene el historial de backups con paginación y filtros.

**Autorización**: Admin, Manager, Viewer

**Parámetros Query**:
- `pageNumber` (int, opcional): Número de página (default: 1)
- `pageSize` (int, opcional): Tamaño de página (default: 20, max: 100)
- `backupScheduleId` (Guid, opcional): Filtrar por ID de programación
- `databaseConnectionId` (Guid, opcional): Filtrar por ID de conexión de BD
- `status` (BackupStatus, opcional): Filtrar por estado (Pending, InProgress, Completed, Failed, Cancelled)
- `isInstantBackup` (bool, opcional): Filtrar por backups instantáneos
- `startDateFrom` (DateTime, opcional): Fecha de inicio desde
- `startDateTo` (DateTime, opcional): Fecha de inicio hasta
- `sortBy` (string, opcional): Campo de ordenamiento (default: "StartTime")
- `sortDirection` (string, opcional): Dirección de ordenamiento: ASC o DESC (default: "DESC")

**Respuesta**: `GetBackupHistoryResult`
```json
{
  "items": [
    {
      "id": "guid",
      "jobId": "guid",
      "databaseConnectionId": "guid",
      "databaseConnectionName": "Production DB",
      "backupScheduleId": "guid",
      "backupScheduleName": "Daily Backup",
      "status": "Completed",
      "statusText": "Completed",
      "startTime": "2024-12-02T10:00:00Z",
      "endTime": "2024-12-02T10:05:00Z",
      "duration": "00:05:00",
      "blobUrl": "https://storage.blob.core.windows.net/...",
      "blobName": "backup_20241202_100000.sql.gz",
      "backupSizeBytes": 1048576,
      "backupSizeMB": 1.0,
      "backupSizeGB": 0.001,
      "errorMessage": null,
      "errorCode": null,
      "retryCount": 0,
      "compressionType": "GZIP",
      "isInstantBackup": false,
      "createdAt": "2024-12-02T10:00:00Z"
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**Ejemplo**:
```bash
# Obtener primera página
GET /api/backuphistory?pageNumber=1&pageSize=20

# Filtrar por estado fallido
GET /api/backuphistory?status=Failed&pageSize=50

# Filtrar por rango de fechas
GET /api/backuphistory?startDateFrom=2024-12-01&startDateTo=2024-12-02

# Filtrar backups instantáneos
GET /api/backuphistory?isInstantBackup=true
```

---

### 2. GET /api/backuphistory/{id}
**Descripción**: Obtiene un registro de historial de backup por ID.

**Autorización**: Admin, Manager, Viewer

**Parámetros Path**:
- `id` (Guid): ID del registro de historial

**Respuesta**: `BackupHistoryDto`
```json
{
  "id": "guid",
  "jobId": "guid",
  "databaseConnectionId": "guid",
  "databaseConnectionName": "Production DB",
  "backupScheduleId": "guid",
  "backupScheduleName": "Daily Backup",
  "status": "Completed",
  "statusText": "Completed",
  "startTime": "2024-12-02T10:00:00Z",
  "endTime": "2024-12-02T10:05:00Z",
  "duration": "00:05:00",
  "blobUrl": "https://storage.blob.core.windows.net/...",
  "blobName": "backup_20241202_100000.sql.gz",
  "backupSizeBytes": 1048576,
  "backupSizeMB": 1.0,
  "backupSizeGB": 0.001,
  "errorMessage": null,
  "errorCode": null,
  "retryCount": 0,
  "compressionType": "GZIP",
  "isInstantBackup": false,
  "createdAt": "2024-12-02T10:00:00Z"
}
```

**Ejemplo**:
```bash
GET /api/backuphistory/550e8400-e29b-41d4-a716-446655440000
```

---

### 3. GET /api/backuphistory/statistics
**Descripción**: Obtiene estadísticas de backups.

**Autorización**: Admin, Manager, Viewer

**Parámetros Query**:
- `backupScheduleId` (Guid, opcional): Filtrar por ID de programación
- `databaseConnectionId` (Guid, opcional): Filtrar por ID de conexión de BD
- `startDate` (DateTime, opcional): Fecha de inicio para estadísticas
- `endDate` (DateTime, opcional): Fecha de fin para estadísticas

**Respuesta**: `BackupStatisticsDto`
```json
{
  "totalBackups": 250,
  "successfulBackups": 230,
  "failedBackups": 15,
  "inProgressBackups": 5,
  "successRate": 92.0,
  "totalBackupSizeBytes": 10737418240,
  "totalBackupSizeMB": 10240.0,
  "totalBackupSizeGB": 10.0,
  "averageDuration": "00:05:30",
  "minDuration": "00:02:00",
  "maxDuration": "00:15:00",
  "lastSuccessfulBackup": "2024-12-02T10:00:00Z",
  "lastFailedBackup": "2024-12-01T15:30:00Z",
  "statusBreakdown": [
    {
      "status": "Completed",
      "statusText": "Completed",
      "count": 230,
      "percentage": 92.0
    },
    {
      "status": "Failed",
      "statusText": "Failed",
      "count": 15,
      "percentage": 6.0
    }
  ],
  "dailyBackupCounts": [
    {
      "date": "2024-12-01T00:00:00Z",
      "totalCount": 12,
      "successCount": 11,
      "failedCount": 1
    }
  ]
}
```

**Ejemplo**:
```bash
# Estadísticas generales
GET /api/backuphistory/statistics

# Estadísticas de una base de datos específica
GET /api/backuphistory/statistics?databaseConnectionId=550e8400-e29b-41d4-a716-446655440000

# Estadísticas del último mes
GET /api/backuphistory/statistics?startDate=2024-11-01&endDate=2024-12-01
```

---

### 4. POST /api/backuphistory/{id}/retry
**Descripción**: Reintenta un backup fallido.

**Autorización**: Admin, Manager

**Parámetros Path**:
- `id` (Guid): ID del registro de historial de backup a reintentar

**Respuesta**:
```json
{
  "message": "Backup retry initiated successfully",
  "originalBackupId": "guid",
  "newJobId": "guid"
}
```

**Ejemplo**:
```bash
POST /api/backuphistory/550e8400-e29b-41d4-a716-446655440000/retry
```

---

### 5. DELETE /api/backuphistory/{id}
**Descripción**: Elimina un registro de historial de backup.

**Autorización**: Admin

**Parámetros Path**:
- `id` (Guid): ID del registro a eliminar

**Respuesta**:
```json
{
  "message": "Backup history deleted successfully"
}
```

**Ejemplo**:
```bash
DELETE /api/backuphistory/550e8400-e29b-41d4-a716-446655440000
```

---

### 6. POST /api/backuphistory/bulk-delete
**Descripción**: Elimina múltiples registros de historial de backup.

**Autorización**: Admin

**Body**:
```json
[
  "550e8400-e29b-41d4-a716-446655440000",
  "550e8400-e29b-41d4-a716-446655440001",
  "550e8400-e29b-41d4-a716-446655440002"
]
```

**Respuesta**:
```json
{
  "message": "Successfully deleted 3 of 3 backup history records",
  "deletedCount": 3,
  "totalRequested": 3
}
```

**Ejemplo**:
```bash
POST /api/backuphistory/bulk-delete
Content-Type: application/json

[
  "550e8400-e29b-41d4-a716-446655440000",
  "550e8400-e29b-41d4-a716-446655440001"
]
```

---

### 7. GET /api/backuphistory/{id}/download
**Descripción**: Obtiene la URL de descarga del archivo de backup desde Azure Blob Storage.

**Autorización**: Admin, Manager

**Parámetros Path**:
- `id` (Guid): ID del registro de historial de backup

**Respuesta**:
```json
{
  "blobUrl": "https://storage.blob.core.windows.net/...",
  "blobName": "backup_20241202_100000.sql.gz",
  "backupSizeBytes": 1048576,
  "backupSizeMB": 1.0,
  "message": "Use the provided URL to download the backup file"
}
```

**Ejemplo**:
```bash
GET /api/backuphistory/550e8400-e29b-41d4-a716-446655440000/download
```

---

### 8. GET /api/backuphistory/by-database
**Descripción**: Obtiene el historial de backups agrupado por conexión de base de datos.

**Autorización**: Admin, Manager, Viewer

**Parámetros Query**:
- `startDate` (DateTime, opcional): Fecha de inicio del filtro
- `endDate` (DateTime, opcional): Fecha de fin del filtro

**Respuesta**:
```json
[
  {
    "databaseConnectionId": "guid",
    "databaseConnectionName": "Production DB",
    "totalBackups": 50,
    "successfulBackups": 48,
    "failedBackups": 2,
    "lastBackup": {
      "id": "guid",
      "startTime": "2024-12-02T10:00:00Z",
      "status": "Completed",
      ...
    },
    "totalSizeGB": 5.2,
    "backups": [
      {
        "id": "guid",
        "startTime": "2024-12-02T10:00:00Z",
        "status": "Completed",
        ...
      }
    ]
  }
]
```

**Ejemplo**:
```bash
# Agrupar por base de datos
GET /api/backuphistory/by-database

# Con filtro de fecha
GET /api/backuphistory/by-database?startDate=2024-11-01&endDate=2024-12-01
```

---

## Estados de Backup (BackupStatus)

- `Pending`: Backup en cola, esperando ser procesado
- `InProgress`: Backup en ejecución
- `Completed`: Backup completado exitosamente
- `Failed`: Backup falló
- `Cancelled`: Backup cancelado

---

## Roles y Permisos

| Endpoint | Admin | Manager | Viewer |
|----------|-------|---------|--------|
| GET /api/backuphistory | ✓ | ✓ | ✓ |
| GET /api/backuphistory/{id} | ✓ | ✓ | ✓ |
| GET /api/backuphistory/statistics | ✓ | ✓ | ✓ |
| POST /api/backuphistory/{id}/retry | ✓ | ✓ | ✗ |
| DELETE /api/backuphistory/{id} | ✓ | ✗ | ✗ |
| POST /api/backuphistory/bulk-delete | ✓ | ✗ | ✗ |
| GET /api/backuphistory/{id}/download | ✓ | ✓ | ✗ |
| GET /api/backuphistory/by-database | ✓ | ✓ | ✓ |

---

## Códigos de Respuesta HTTP

- `200 OK`: Operación exitosa
- `400 Bad Request`: Parámetros inválidos
- `401 Unauthorized`: No autenticado
- `403 Forbidden`: Sin permisos suficientes
- `404 Not Found`: Recurso no encontrado
- `500 Internal Server Error`: Error del servidor

---

## Ejemplos de Uso Completos

### Ejemplo 1: Monitorear Backups Fallidos
```bash
# 1. Obtener lista de backups fallidos
GET /api/backuphistory?status=Failed&pageSize=50

# 2. Ver detalles de un backup fallido
GET /api/backuphistory/{id}

# 3. Reintentar el backup
POST /api/backuphistory/{id}/retry

# 4. Verificar el nuevo intento
GET /api/backuphistory?jobId={newJobId}
```

### Ejemplo 2: Dashboard de Estadísticas
```bash
# 1. Obtener estadísticas generales
GET /api/backuphistory/statistics

# 2. Obtener resumen por base de datos
GET /api/backuphistory/by-database

# 3. Obtener estadísticas del último mes para una BD específica
GET /api/backuphistory/statistics?databaseConnectionId={id}&startDate=2024-11-01
```

### Ejemplo 3: Limpieza de Backups Antiguos
```bash
# 1. Obtener backups antiguos
GET /api/backuphistory?startDateTo=2024-01-01&pageSize=100

# 2. Eliminar backups seleccionados en lote
POST /api/backuphistory/bulk-delete
Content-Type: application/json
[
  "id1", "id2", "id3", ...
]
```

### Ejemplo 4: Descargar Backup
```bash
# 1. Buscar backup específico
GET /api/backuphistory?databaseConnectionId={id}&startDateFrom=2024-12-01

# 2. Obtener URL de descarga
GET /api/backuphistory/{id}/download

# 3. Usar la URL retornada para descargar el archivo
# (El cliente usa el blobUrl retornado para descargar directamente desde Azure)
```

---

## Notas Importantes

1. **Paginación**: Los resultados están paginados por defecto con un máximo de 100 registros por página.

2. **Filtros**: Todos los filtros son opcionales y pueden combinarse para consultas más específicas.

3. **Ordenamiento**: Por defecto se ordena por `StartTime DESC` (más recientes primero).

4. **Reintentos**: Al reintentar un backup, se crea un nuevo backup instantáneo con la misma configuración del original.

5. **Eliminación**: La eliminación es permanente y no se puede deshacer. Solo usuarios Admin pueden eliminar registros.

6. **Descargas**: Las URLs de descarga apuntan a Azure Blob Storage. En producción, considere implementar tokens SAS con expiración.

7. **Estadísticas**: Las estadísticas de conteos diarios incluyen los últimos 30 días por defecto.

8. **Tenant Context**: Todos los endpoints operan en el contexto del tenant actual (extraído del JWT).
