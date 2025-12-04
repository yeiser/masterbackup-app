# MasterBackup Worker - Docker Setup

## 🐳 Debugging con Visual Studio 2022

### Opción 1: Ejecutar en Docker con F5 (Recomendado)

1. **Abrir el proyecto Worker en Visual Studio 2022**

2. **Seleccionar "Docker" como target de debug**
   - En la barra superior, buscar el dropdown donde dice "MasterBackup-Worker"
   - Cambiar a "Docker"

3. **Presionar F5**
   - Visual Studio construirá automáticamente la imagen Docker
   - Iniciará el contenedor con el debugger adjunto
   - Podrás poner breakpoints y debuggear normalmente

4. **Ver logs**
   - Output Window → Container Tools
   - Output Window → Docker

### Opción 2: Ejecutar localmente (sin Docker)

1. **Instalar PostgreSQL client tools:**
   ```powershell
   winget install PostgreSQL.PostgreSQL
   ```

2. **Actualizar `launchSettings.json` con las rutas correctas de pg_dump**

3. **Seleccionar "MasterBackup-Worker" en la barra superior**

4. **Presionar F5**

## 🔧 Build Manual

### Build image
```powershell
docker build -t masterbackup-worker:latest .
```

### Run with docker-compose
```powershell
docker-compose up -d
```

### View logs
```powershell
docker-compose logs -f worker
```

### Stop
```powershell
docker-compose down
```

## 🧪 Verificación

### Test pg_dump availability
```powershell
docker exec masterbackup-worker-debug pg_dump --version
```

### Test pg_restore availability
```powershell
docker exec masterbackup-worker-debug pg_restore --version
```

## 📝 Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `Worker__TenantId` | Tenant GUID | - | ✅ |
| `Worker__WorkerName` | Worker identifier | Worker-01 | ✅ |
| `Worker__ApiKey` | API Key for authentication | - | ✅ |
| `Api__BaseUrl` | API endpoint | https://localhost:7001 | ✅ |
| `RABBITMQ_HOST` | RabbitMQ host | localhost | ✅ |
| `RABBITMQ_PORT` | RabbitMQ port | 5672 | ✅ |
| `AZURE_STORAGE_CONNECTION_STRING` | Azure Storage connection | UseDevelopmentStorage=true | ✅ |
| `DatabaseTools__PostgreSQL__PgDumpPath` | Path to pg_dump | /usr/bin/pg_dump | ✅ |
| `DatabaseTools__PostgreSQL__PgRestorePath` | Path to pg_restore | /usr/bin/pg_restore | ✅ |

## ✅ Included in Docker Image

- ✅ .NET 8 Runtime
- ✅ PostgreSQL Client 16 (pg_dump, pg_restore)
- ✅ RabbitMQ Client
- ✅ Azure Storage SDK

## 📁 Volume Mounts

- `./backups:/app/backups` - Backup storage directory
- `~/.vsdbg:/remote_debugger:rw` - Visual Studio remote debugger

## 🐛 Debugging Features

- ✅ **Breakpoints**: Funcionan normalmente
- ✅ **Variables**: Inspeccionables en tiempo real
- ✅ **Call Stack**: Visible
- ✅ **Logs**: En Output Window
- ⚠️ **Hot Reload**: No disponible (limitación de Docker)

## 🔍 Troubleshooting

### Error: "pg_dump not found"
```powershell
# Verificar que pg_dump está en el contenedor
docker exec masterbackup-worker-debug which pg_dump

# Si falla, reconstruir la imagen
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Error: "Cannot connect to API"
- Asegúrate de que el API está corriendo en el host
- Verifica que `host.docker.internal` resuelve correctamente
- En Linux, puede ser necesario usar `172.17.0.1` en lugar de `host.docker.internal`

### Error: "Cannot connect to RabbitMQ"
```powershell
# Verificar que RabbitMQ está corriendo
docker ps | grep rabbitmq

# Verificar conectividad desde el contenedor
docker exec masterbackup-worker-debug ping -c 3 host.docker.internal
```

## 🚀 Quick Start

```powershell
# Clonar y navegar al directorio
cd MasterBackup-Worker

# Opción A: Usar Visual Studio 2022
# 1. Abrir MasterBackup-Worker.csproj
# 2. Seleccionar "Docker" como target
# 3. Presionar F5

# Opción B: Usar línea de comandos
docker-compose up --build

# Ver logs
docker-compose logs -f worker
```

## 📚 Referencias

- [Docker Support in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/containers/)
- [Debugging with Docker in VS](https://docs.microsoft.com/en-us/visualstudio/containers/edit-and-refresh)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
