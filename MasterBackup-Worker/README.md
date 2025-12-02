# MasterBackup Worker

Worker service para ejecutar backups de bases de datos y pruebas de conexión en el sistema MasterBackup.

## 🏗️ Arquitectura

El Worker implementa **Clean Architecture** con las siguientes capas:

```
MasterBackup-Worker/
├── Domain/                          # Capa de Dominio (Entities, Enums)
│   ├── Entities/
│   │   ├── TestConnectionMessage.cs
│   │   ├── BackupJobMessage.cs
│   │   └── WorkerConfiguration.cs
│   └── Enums/
│       ├── WorkerAssignmentMode.cs
│       └── DatabaseType.cs
│
├── Application/                     # Capa de Aplicación (Interfaces, Servicios)
│   ├── Interfaces/
│   │   ├── IWorkerAuthorizationService.cs
│   │   ├── IConnectionTestService.cs
│   │   ├── IBackupExecutorService.cs
│   │   └── IApiClient.cs
│   └── Services/
│       ├── WorkerAuthorizationService.cs    ⭐ Validación de asignación
│       ├── ConnectionTestService.cs
│       └── BackupExecutorService.cs
│
├── Infrastructure/                  # Capa de Infraestructura
│   ├── MessageQueue/
│   │   └── RabbitMQConsumerService.cs
│   ├── BackupStrategies/
│   │   └── (Estrategias de backup por motor)
│   └── Http/
│       └── ApiClient.cs
│
└── Program.cs                       # Punto de entrada
```

## ⚙️ Configuración

### ✨ Configuración Simplificada

Solo necesitas configurar el **ApiKey** del tenant. El `WorkerId` y `TenantId` se obtienen **automáticamente** del API durante el registro.

**appsettings.json:**

```json
{
  "Worker": {
    "WorkerName": "Worker-Production-01",
    "ApiKey": "api-key-del-tenant",
    "Tags": ["production", "postgresql", "americas"],
    "SupportedDatabaseTypes": ["PostgreSQL"],
    "MaxConcurrentJobs": 3
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672
  },
  "Api": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

### Cómo Obtener el ApiKey

**Opción 1 - Query SQL en la base de datos Master:**
```sql
SELECT "Id", "Name", "ApiKey" 
FROM "Tenants" 
WHERE "IsActive" = true;
```

**Opción 2 - Desde el frontend (futuro):**
- Iniciar sesión como Admin
- Ir a Configuración → API Settings
- Copiar el ApiKey mostrado

### Variables de Entorno (Recomendado para Docker)

```bash
WORKER__WORKERNAME=Worker-Production-01
WORKER__APIKEY=api-key-del-tenant
WORKER__TAGS__0=production
WORKER__TAGS__1=postgresql
WORKER__TAGS__2=americas
WORKER__SUPPORTEDDATABASETYPES__0=PostgreSQL
WORKER__MAXCONCURRENTJOBS=3
RABBITMQ__HOST=rabbitmq
RABBITMQ__PORT=5672
API__BASEURL=https://api:8080
```

## 🔐 Sistema de Autorización de Workers

El Worker implementa validación de asignación en **dos modos**:

### 1. Modo Dedicado (Dedicated)

El job está explícitamente asignado a un worker específico.

**Validación:**
- ✅ El `AssignedWorkerId` del mensaje debe coincidir con el `WorkerId` del worker
- ✅ Solo este worker puede procesar el job

**Caso de uso:** Bases de datos sensibles de producción.

**Ejemplo de mensaje:**
```json
{
  "ConnectionId": "guid",
  "AssignmentMode": "Dedicated",
  "AssignedWorkerId": "guid-del-worker-asignado",
  "Tags": []
}
```

### 2. Modo Automático (Auto)

El job puede ser procesado por cualquier worker con tags coincidentes.

**Validación:**
- ✅ El worker debe tener al menos un tag que coincida con los tags del job
- ✅ Permite load balancing automático entre workers

**Caso de uso:** Ambientes de desarrollo/testing, alta disponibilidad.

**Ejemplo de mensaje:**
```json
{
  "ConnectionId": "guid",
  "AssignmentMode": "Auto",
  "AssignedWorkerId": null,
  "Tags": ["production", "postgresql"]
}
```

### Flujo de Autorización

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Mensaje recibido de RabbitMQ                            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. WorkerAuthorizationService.CanProcessJobAsync()         │
│    - Valida TenantId                                        │
│    - Valida DatabaseType (para backups)                    │
│    - Valida modo de asignación                             │
└────────────────────┬────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
        ▼                         ▼
┌──────────────────┐    ┌──────────────────────┐
│ Modo Dedicado    │    │ Modo Auto            │
│ - WorkerId debe  │    │ - Tags deben         │
│   coincidir      │    │   intersectar        │
└────────┬─────────┘    └──────────┬───────────┘
         │                         │
         └────────────┬────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. ¿Autorizado?                                             │
│    ✓ Sí → Procesar job y ACK mensaje                       │
│    ✗ No → Rechazar y reencolar (NACK con requeue=true)     │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Ejecución

### Desarrollo Local

```bash
# Restaurar paquetes
dotnet restore

# Ejecutar
dotnet run
```

### Producción (Docker)

```bash
# Build
docker build -t masterbackup-worker:latest .

# Run
docker run -d \
  --name worker-01 \
  -e WORKER__WORKERID=... \
  -e WORKER__TENANTID=... \
  -e WORKER__APIKEY=... \
  masterbackup-worker:latest
```

## 📋 Funcionalidades Implementadas

### ✅ Completado:

- ✅ **WorkerAuthorizationService**: Validación de asignación (Dedicado/Auto)
- ✅ **ConnectionTestService**: Prueba de conexión a PostgreSQL
- ✅ **RabbitMQConsumerService**: Consumidor de mensajes con autorización
- ✅ **Clean Architecture**: Separación de capas Domain/Application/Infrastructure
- ✅ **Logging estructurado**: Logs detallados en cada paso
- ✅ **Configuración flexible**: appsettings.json + variables de entorno
- ✅ **Reencolar mensajes**: Jobs no autorizados se reencolan para otros workers

### ⏳ Pendiente:

- ⏳ **BackupExecutorService**: Implementación completa de pg_dump
- ⏳ **ApiClient**: Llamadas HTTP reales a la API
- ⏳ **Azure Blob Storage**: Upload de archivos de backup
- ⏳ **MySQL/SQL Server**: Soporte para otros motores
- ⏳ **Heartbeat**: Envío automático cada 30 segundos
- ⏳ **Registro automático**: Auto-registro en el API al iniciar

## 🧪 Testing

### Enviar mensaje de prueba a RabbitMQ:

```bash
# Instalar RabbitMQ localmente
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Enviar mensaje (usando rabbitmqadmin o script)
# Ver RabbitMQ Management UI: http://localhost:15672 (guest/guest)
```

### Logs esperados:

```
✓ Worker initialized successfully
✓ Listening for jobs on RabbitMQ...

info: RabbitMQConsumerService[0]
      Received message from queue {tenantId}.backup.queue

info: WorkerAuthorizationService[0]
      Worker {WorkerId} authorized to process test connection {ConnectionId} (Mode: Auto)

info: ConnectionTestService[0]
      Testing connection to PostgreSQL database: localhost:5432/mydb

info: ConnectionTestService[0]
      Successfully connected to PostgreSQL: PostgreSQL 15.2 on x86_64-pc-linux-gnu

info: RabbitMQConsumerService[0]
      Test connection {ConnectionId} completed: SUCCESS
```

## 🔧 Troubleshooting

### Worker no procesa mensajes:

1. **Verificar WorkerId y TenantId**: Deben coincidir con el tenant y worker registrados
2. **Verificar Tags**: En modo Auto, debe haber al menos un tag coincidente
3. **Verificar RabbitMQ**: Worker debe estar conectado a la queue correcta: `{tenantId}.backup.queue`
4. **Logs de autorización**: Revisar `GetAuthorizationFailureReason()` en logs

### Mensajes se reencolan infinitamente:

- Si ningún worker está autorizado, los mensajes se reencolarán continuamente
- Solución: Asignar un worker dedicado o agregar tags correctos a algún worker

### ⚠️ El Worker no consume mensajes de RabbitMQ:

**Síntoma**: Los mensajes se publican en RabbitMQ pero el worker no los procesa.

**Causa más común**: El `TenantId` en `appsettings.json` es incorrecto o está como `00000000-0000-0000-0000-000000000000`.

**Solución**:

1. Ejecuta el script para obtener el TenantId correcto:
   ```powershell
   .\get-tenant-info.ps1
   ```

2. O ejecuta este query en PostgreSQL:
   ```sql
   SELECT "Id" as TenantId, "Name", "ApiKey" 
   FROM "Tenants" 
   WHERE "IsActive" = true;
   ```

3. Actualiza `appsettings.json` con el TenantId y ApiKey correctos:
   ```json
   {
     "Worker": {
       "TenantId": "COPIA_EL_GUID_REAL_AQUI",
       "ApiKey": "COPIA_EL_APIKEY_REAL_AQUI",
       ...
     }
   }
   ```

4. Reinicia el worker. Deberías ver en los logs:
   ```
   Queue Name: {tu-tenant-id-real}.test-connection.queue
   ```

**Cómo verificar**: 
- En los logs del worker, busca la línea `Queue Name:`
- El TenantId en el nombre de la queue debe coincidir con el TenantId del usuario que hace el test
- Si ves `00000000-0000-0000-0000-000000000000`, el TenantId está mal configurado

## 📚 Referencias

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [RabbitMQ Best Practices](https://www.rabbitmq.com/best-practices.html)
- [Npgsql Documentation](https://www.npgsql.org/doc/)

## 📝 Notas de Implementación

### Principios SOLID:

- **Single Responsibility**: Cada servicio tiene una única responsabilidad
- **Open/Closed**: Estrategias de backup extensibles sin modificar código existente
- **Dependency Inversion**: Todos los servicios dependen de abstracciones (interfaces)

### Buenas Prácticas:

- ✅ Inyección de dependencias con IServiceProvider
- ✅ Logging estructurado con contexto
- ✅ Configuración externalizada
- ✅ Manejo de errores con try-catch y logs
- ✅ Documentación XML en código
- ✅ Nombres descriptivos y convenciones .NET
