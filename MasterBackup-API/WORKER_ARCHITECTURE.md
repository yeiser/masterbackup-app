# Nueva Arquitectura de Test de Conexiones - Worker-Based

## Resumen

Se ha refactorizado completamente el sistema de prueba de conexiones de base de datos para que el **Worker** (on-premise) sea quien realice las pruebas, en lugar del API. Esto es correcto porque el Worker está en la red local del cliente y tiene acceso directo a las bases de datos.

## Flujo de Trabajo

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Frontend  │─────>│     API     │─────>│  RabbitMQ   │─────>│   Worker    │
│  (Angular)  │      │   (.NET 8)  │      │   (Queue)   │      │ (On-Premise)│
└─────────────┘      └─────────────┘      └─────────────┘      └─────────────┘
      ^                                                                 │
      │                                                                 │
      │                          ┌──────────────────────────────────────┘
      │                          │ Test Result
      │                          ▼
      │              ┌─────────────────────┐
      └──────────────│   API (callback)    │
         Notification│ /test-result update │
                     └─────────────────────┘
```

### Paso a Paso

1. **Usuario hace clic en "Probar Conexión"** en el frontend
2. **Frontend envía POST** `/api/database-connections/{id}/test`
3. **API:**
   - Busca la conexión en TenantDbContext
   - Desencripta la contraseña
   - Crea mensaje `TestConnectionMessage` con todos los datos
   - Publica mensaje en RabbitMQ queue: `{tenantId}.test-connection.queue`
   - Retorna `202 Accepted` al frontend
4. **Frontend muestra mensaje:** "Probando conexión... El resultado llegará en breve"
5. **Worker (on-premise):**
   - Consume mensaje de RabbitMQ
   - Realiza test de conexión a la base de datos
   - Envía resultado a API: `PUT /api/database-connections/{id}/test-result`
6. **API recibe resultado:**
   - Actualiza `LastTestedAt`, `LastTestSuccessful`, `LastTestStatus`, `EngineVersion`
   - (Opcional) Envía notificación vía SignalR al frontend
7. **Frontend actualiza UI** automáticamente o con refresh manual

## Archivos Creados/Modificados

### Backend (.NET API)

#### Nuevos Archivos

1. **`Application/Common/DTOs/TestConnectionMessage.cs`**
   - DTO para el mensaje enviado a RabbitMQ
   - Contiene: `ConnectionId`, `TenantId`, `Type`, `Host`, `Port`, `Database`, `Username`, `Password`, `SSLMode`

2. **`Application/Common/Interfaces/IMessageQueueService.cs`**
   - Interface para gestión de RabbitMQ
   - Métodos: `PublishTestConnectionJob`, `CreateTenantQueue`, `DeleteTenantQueue`

3. **`Infrastructure/Services/RabbitMQService.cs`**
   - Implementación de IMessageQueueService
   - Usa RabbitMQ.Client 7.2.0 (API async)
   - Configura conexión desde appsettings.json

4. **`Application/Features/DatabaseConnections/Commands/UpdateTestResultCommand.cs`**
   - Command para actualizar resultado del test
   - Propiedades: `ConnectionId`, `Success`, `Status`, `ServerVersion`

5. **`Application/Features/DatabaseConnections/Commands/UpdateTestResultCommandHandler.cs`**
   - Handler que actualiza DatabaseConnection con resultado del test
   - Actualiza: `LastTestedAt`, `LastTestSuccessful`, `LastTestStatus`, `EngineVersion`

#### Archivos Modificados

1. **`Application/Features/DatabaseConnections/Commands/TestDatabaseConnectionCommand.cs`**
   - **ANTES:** Recibía todos los datos de conexión, retornaba `TestConnectionResultDto`
   - **AHORA:** Solo recibe `ConnectionId`, retorna `bool`

2. **`Application/Features/DatabaseConnections/Commands/TestDatabaseConnectionCommandHandler.cs`**
   - **ANTES:** Llamaba directamente a `IDatabaseConnectionTester` para probar
   - **AHORA:** 
     - Obtiene conexión de TenantDbContext
     - Desencripta password con `IEncryptionService`
     - Crea `TestConnectionMessage`
     - Publica en RabbitMQ vía `IMessageQueueService`
     - Retorna `true` si se envió correctamente

3. **`Presentation/Controllers/DatabaseConnectionsController.cs`**
   - **Eliminado:** `POST /api/database-connections/test` (test sin guardar)
   - **Eliminado:** `POST /api/database-connections/{id}/test` (antigua implementación)
   - **Agregado:** `POST /api/database-connections/{id}/test` (envía job a Worker)
   - **Agregado:** `PUT /api/database-connections/{id}/test-result` (callback del Worker)

4. **`Application/Common/Interfaces/ITenantService.cs`**
   - **Agregado:** Método `Guid GetCurrentTenantId()`

5. **`Infrastructure/Services/TenantService.cs`**
   - **Agregado:** Implementación de `GetCurrentTenantId()` usando `ITenantContext`

6. **`Program.cs`**
   - **Eliminado:** Registro de `IDatabaseConnectionTester`
   - **Agregado:** Registro de `IMessageQueueService` como Singleton

7. **`appsettings.json`**
   - **Agregado:** Sección `RabbitMQ` con configuración:
     ```json
     "RabbitMQ": {
       "HostName": "localhost",
       "Port": "5672",
       "UserName": "guest",
       "Password": "guest",
       "VirtualHost": "/"
     }
     ```

#### Archivos Eliminados (ya no se usan)

- `Application/Common/Interfaces/IDatabaseConnectionTester.cs`
- `Infrastructure/Services/DatabaseConnectionTester.cs`
- `Application/Features/DatabaseConnections/Commands/TestExistingConnectionCommand.cs`
- `Application/Features/DatabaseConnections/Commands/TestExistingConnectionCommandHandler.cs`

### Frontend (Angular)

#### Archivos Modificados

1. **`core/services/database-connection.service.ts`**
   - **Eliminado:** `testConnection(dto)` - probaba sin guardar
   - **Eliminado:** `testExistingConnection(id)` - retornaba resultado inmediato
   - **Agregado:** `testConnection(id): Observable<void>` - envía job al Worker

2. **`features/databases/components/databases.component.ts`**
   - **Eliminado:** Variable `testResult`
   - **Eliminado:** Método `testConnectionBeforeSave()`
   - **Eliminado:** Método `testExistingConnection()` antiguo
   - **Modificado:** Método `testExistingConnection()` nuevo:
     - Envía request al API
     - Muestra mensaje informativo
     - NO espera resultado inmediato

3. **`features/databases/components/databases.component.html`**
   - **Eliminado:** Botón "Probar Conexión" del modal de formulario
   - **Eliminado:** Modal de resultado de test (`testModal`)

## Configuración Requerida

### RabbitMQ

El sistema ahora requiere RabbitMQ instalado y corriendo:

```bash
# Docker (recomendado)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# O instalar localmente
# Windows: https://www.rabbitmq.com/install-windows.html
# Linux: apt-get install rabbitmq-server
```

### Variables de Entorno (Opcional)

```bash
# API
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
```

## Estructura de Colas RabbitMQ

### Queue por Tenant

Cada tenant tiene su propia queue para test de conexiones:

**Formato:** `{tenantId}.test-connection.queue`

**Ejemplo:** `550e8400-e29b-41d4-a716-446655440000.test-connection.queue`

### Mensaje JSON

```json
{
  "connectionId": "guid",
  "tenantId": "guid",
  "type": 1,
  "host": "localhost",
  "port": 5432,
  "database": "mydb",
  "username": "postgres",
  "password": "decrypted-password",
  "sslMode": "prefer"
}
```

## API Endpoints

### Para el Frontend

```http
POST /api/database-connections/{id}/test
Authorization: Bearer {token}
```

**Response:** `202 Accepted`
```json
{
  "message": "Test connection job sent to worker. Result will arrive via SignalR."
}
```

### Para el Worker

```http
PUT /api/database-connections/{id}/test-result
Content-Type: application/json

{
  "success": true,
  "status": "Connection successful",
  "serverVersion": "PostgreSQL 14.5"
}
```

**Response:** `200 OK`
```json
{
  "message": "Test result updated successfully"
}
```

## Próximos Pasos

### 1. Worker Implementation

Crear aplicación Worker (Console App .NET 8) que:

- Se conecte a RabbitMQ
- Consuma mensajes de `{tenantId}.test-connection.queue`
- Ejecute tests de conexión usando librerías nativas:
  - Npgsql para PostgreSQL
  - MySqlConnector para MySQL
  - Microsoft.Data.SqlClient para SQL Server
  - MongoDB.Driver para MongoDB
- Envíe resultados a API via `PUT /test-result`

### 2. SignalR Implementation (Opcional pero recomendado)

Para notificaciones en tiempo real:

- Implementar SignalR Hub en API
- Conectar frontend a SignalR
- Enviar notificaciones cuando lleguen resultados
- Actualizar UI automáticamente

### 3. Retry Logic

Implementar reintentos en caso de fallo:

- Configurar Dead Letter Queue en RabbitMQ
- Implementar exponential backoff
- Logging de fallos para debugging

## Ventajas de esta Arquitectura

1. ✅ **Seguridad:** Worker está en red local, tiene acceso a DBs internas
2. ✅ **Escalabilidad:** Múltiples workers pueden consumir misma queue
3. ✅ **Resiliencia:** RabbitMQ garantiza entrega de mensajes
4. ✅ **Desacoplamiento:** API no se bloquea esperando tests lentos
5. ✅ **Multi-tenant:** Queue por tenant permite priorización
6. ✅ **Monitoreo:** RabbitMQ Management UI para ver estado de colas

## Dependencias Agregadas

```xml
<PackageReference Include="RabbitMQ.Client" Version="7.2.0" />
```

## Testing

### Prueba Manual

1. Iniciar RabbitMQ: `docker start rabbitmq`
2. Iniciar API: `dotnet run`
3. Crear conexión de BD en frontend
4. Click en "Probar Conexión"
5. Verificar mensaje en RabbitMQ Management (http://localhost:15672)
6. Worker consume y envía resultado
7. Refrescar lista para ver resultado actualizado

---

**Fecha de implementación:** 28 de Noviembre, 2025
**Versión API:** .NET 8
**Versión RabbitMQ.Client:** 7.2.0
