# Variables de Entorno - MasterBackup API

Este documento describe las variables de entorno utilizadas por la API de MasterBackup.

## Variables Disponibles

### 1. Conexión a Base de Datos Master

**Variable:** `MASTER_DATABASE_CONNECTION`

**Descripción:** Connection string para la base de datos PostgreSQL master que gestiona los tenants.

**Formato:**
```
Host=<host>;Port=<puerto>;Database=<nombre_db>;Username=<usuario>;Password=<contraseña>;Include Error Detail=true
```

**Ejemplo:**
```bash
MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=mipassword123;Include Error Detail=true"
```

**Fallback:** Si no se define, se usa el valor de `ConnectionStrings:MasterDatabase` en `appsettings.json`

---

### 2. Conexión para Logs de Serilog

**Variable:** `MASTER_DATABASE_CONNECTION`

**Descripción:** Connection string para la base de datos PostgreSQL donde se almacenan los logs. Puede ser la misma que la base de datos master o una diferente.

**Formato:**
```
Host=<host>;Port=<puerto>;Database=<nombre_db>;Username=<usuario>;Password=<contraseña>;Include Error Detail=true
```

**Ejemplo:**
```bash
MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=mipassword123;Include Error Detail=true"
```

**Fallback:** Si no se define, se usa el mismo valor que `MASTER_DATABASE_CONNECTION`

---

## Configuración por Entorno

### Desarrollo Local (Windows PowerShell)

```powershell
# Configurar variables de entorno para la sesión actual
$env:MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true"
$env:MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true"

# Ejecutar la API
dotnet run
```

### Desarrollo Local (Windows CMD)

```cmd
set MASTER_DATABASE_CONNECTION=Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true
set MASTER_DATABASE_CONNECTION=Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true

dotnet run
```

### Desarrollo Local (Linux/Mac)

```bash
export MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true"
export MASTER_DATABASE_CONNECTION="Host=localhost;Port=5432;Database=master_saas;Username=postgres;Password=admin;Include Error Detail=true"

dotnet run
```

---

## Uso con Docker

### Archivo `.env`

Crea un archivo `.env` en la raíz del proyecto:

```env
MASTER_DATABASE_CONNECTION=Host=postgres-server;Port=5432;Database=master_saas;Username=postgres;Password=secretpassword;Include Error Detail=true
MASTER_DATABASE_CONNECTION=Host=postgres-server;Port=5432;Database=master_saas;Username=postgres;Password=secretpassword;Include Error Detail=true
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "7001:8080"
    environment:
      - MASTER_DATABASE_CONNECTION=${MASTER_DATABASE_CONNECTION}
      - MASTER_DATABASE_CONNECTION=${MASTER_DATABASE_CONNECTION}
    depends_on:
      - postgres
    
  postgres:
    image: postgres:16
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=secretpassword
      - POSTGRES_DB=master_saas
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### Ejecutar con Docker Compose

```bash
docker-compose --env-file .env up
```

---

## Uso con Azure App Service

1. Ir a Azure Portal
2. Navegar a tu App Service
3. Ir a **Configuration** > **Application settings**
4. Agregar nuevas configuraciones:

```
Name: MASTER_DATABASE_CONNECTION
Value: Host=tu-servidor.postgres.database.azure.com;Port=5432;Database=master_saas;Username=adminuser@tu-servidor;Password=tupassword;SSL Mode=Require

Name: MASTER_DATABASE_CONNECTION
Value: Host=tu-servidor.postgres.database.azure.com;Port=5432;Database=master_saas;Username=adminuser@tu-servidor;Password=tupassword;SSL Mode=Require
```

5. Guardar cambios y reiniciar la aplicación

---

## Uso con AWS Elastic Beanstalk

### Usando AWS Console

1. Ir a Elastic Beanstalk Console
2. Seleccionar tu entorno
3. Ir a **Configuration** > **Software**
4. En **Environment properties** agregar:

```
MASTER_DATABASE_CONNECTION = Host=tu-rds-instance.rds.amazonaws.com;Port=5432;Database=master_saas;Username=postgres;Password=tupassword;SSL Mode=Require
MASTER_DATABASE_CONNECTION = Host=tu-rds-instance.rds.amazonaws.com;Port=5432;Database=master_saas;Username=postgres;Password=tupassword;SSL Mode=Require
```

### Usando EB CLI

```bash
eb setenv MASTER_DATABASE_CONNECTION="Host=tu-rds-instance.rds.amazonaws.com;Port=5432;Database=master_saas;Username=postgres;Password=tupassword;SSL Mode=Require"
eb setenv MASTER_DATABASE_CONNECTION="Host=tu-rds-instance.rds.amazonaws.com;Port=5432;Database=master_saas;Username=postgres;Password=tupassword;SSL Mode=Require"
```

---

## Uso con Kubernetes

Crear un Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: masterbackup-secrets
type: Opaque
stringData:
  master-db-connection: "Host=postgres-service;Port=5432;Database=master_saas;Username=postgres;Password=secretpassword;Include Error Detail=true"
  serilog-db-connection: "Host=postgres-service;Port=5432;Database=master_saas;Username=postgres;Password=secretpassword;Include Error Detail=true"
```

Usar en el Deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: masterbackup-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: masterbackup-api
  template:
    metadata:
      labels:
        app: masterbackup-api
    spec:
      containers:
      - name: api
        image: masterbackup-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: MASTER_DATABASE_CONNECTION
          valueFrom:
            secretKeyRef:
              name: masterbackup-secrets
              key: master-db-connection
        - name: MASTER_DATABASE_CONNECTION
          valueFrom:
            secretKeyRef:
              name: masterbackup-secrets
              key: serilog-db-connection
```

---

## Verificación

Para verificar que las variables de entorno están siendo leídas correctamente:

1. **Logs de inicio:** Al iniciar la API, verás en los logs:
   ```
   [INF] Using database connection: Host=***;Port=5432;Database=master_saas...
   ```
   
2. **Endpoint de health check:** Puedes crear un endpoint que verifique la conexión (sin exponer credenciales)

---

## Seguridad

⚠️ **IMPORTANTE:**

1. **Nunca** commites archivos `.env` o con credenciales al repositorio
2. Agrega `.env` al `.gitignore`
3. Usa secrets managers en producción (Azure Key Vault, AWS Secrets Manager, etc.)
4. Rota las contraseñas periódicamente
5. Usa conexiones SSL/TLS en producción
6. Limita los permisos de la base de datos al mínimo necesario

---

## Prioridad de Configuración

La API busca la configuración en el siguiente orden:

1. **Variables de entorno** (mayor prioridad)
   - `MASTER_DATABASE_CONNECTION`
   - `MASTER_DATABASE_CONNECTION`

2. **appsettings.json** (fallback)
   - `ConnectionStrings:MasterDatabase`
   - `Serilog:WriteTo:PostgreSQL:Args:connectionString`

3. **Error** si ninguna está configurada

---

## Troubleshooting

### Error: "Master database connection string not configured"

**Solución:** Verifica que hayas configurado la variable de entorno o que `appsettings.json` tenga el valor correcto.

### Error: "Could not connect to the database"

**Solución:** 
- Verifica que el servidor PostgreSQL esté ejecutándose
- Verifica host, puerto, nombre de base de datos, usuario y contraseña
- Verifica firewall/security groups
- Verifica que la base de datos existe

### Logs no se guardan en PostgreSQL

**Solución:**
- Verifica que la tabla `Logs` existe en la base de datos
- Ejecuta las migraciones: `dotnet ef database update --context MasterDbContext`
- Verifica la variable `MASTER_DATABASE_CONNECTION`

---

## Ejemplo Completo

```powershell
# 1. Configurar variables de entorno
$env:MASTER_DATABASE_CONNECTION="Host=54.39.107.101;Port=5432;Database=master_saas;Username=postgres;Password=admin123;Include Error Detail=true"

# 2. Ejecutar la API
cd MasterBackup-API
dotnet run

# 3. Verificar en los logs que se conectó correctamente
# Deberías ver: [INF] Using database connection: Host=54.39.107.101...
```

---

## Referencias

- [.NET Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Environment Variables in Docker](https://docs.docker.com/compose/environment-variables/)
- [Npgsql Connection Strings](https://www.npgsql.org/doc/connection-string-parameters.html)
- [Serilog Configuration](https://github.com/serilog/serilog/wiki/Configuration-Basics)
