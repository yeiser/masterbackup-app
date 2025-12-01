# PostgreSQL Backup & Restore en Worker

Este documento describe cómo el Worker utiliza las herramientas de PostgreSQL para realizar backups y restauraciones.

## 🛠️ Herramientas Incluidas en el Contenedor

El Dockerfile del Worker incluye las siguientes herramientas de PostgreSQL:

### Backup:
- **`pg_dump`**: Backup de una base de datos individual
- **`pg_dumpall`**: Backup de todas las bases de datos y roles del cluster

### Restore:
- **`pg_restore`**: Restauración desde archivos de backup en formato custom/tar/directory
- **`psql`**: Restauración desde archivos SQL planos

### Compresión:
- **`gzip`**: Compresión estándar (.gz)
- **`bzip2`**: Compresión alta (.bz2)
- **`xz`**: Compresión máxima (.xz)

## 📋 Uso de pg_dump

### Formato Custom (Recomendado)

El formato custom es el más flexible y permite restauraciones selectivas:

```bash
pg_dump \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --format=custom \
  --file=backup.dump \
  --verbose \
  --no-password
```

**Ventajas:**
- ✅ Compresión incorporada
- ✅ Restauración selectiva de tablas
- ✅ Restauración paralela
- ✅ Formato binario optimizado

### Formato SQL Plano

Para backups legibles y editables:

```bash
pg_dump \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --file=backup.sql \
  --verbose
```

### Formato Directory (Para bases grandes)

Permite backups paralelos para mayor velocidad:

```bash
pg_dump \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --format=directory \
  --file=backup_directory \
  --jobs=4 \
  --verbose
```

## 🔄 Uso de pg_restore

### Restaurar desde Custom Format

```bash
pg_restore \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --verbose \
  --no-owner \
  --no-privileges \
  backup.dump
```

### Restaurar Selectivamente

Restaurar solo ciertas tablas:

```bash
pg_restore \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --table=users \
  --table=orders \
  backup.dump
```

### Restaurar en Paralelo

Para bases de datos grandes:

```bash
pg_restore \
  --host=hostname \
  --port=5432 \
  --username=username \
  --dbname=database \
  --jobs=4 \
  --verbose \
  backup_directory
```

## 💻 Implementación en C# (Worker)

### Ejemplo de Backup con pg_dump

```csharp
public async Task<(bool Success, string FilePath, long FileSize)> ExecutePostgreSQLBackup(
    string host, 
    int port, 
    string database, 
    string username, 
    string password)
{
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    var fileName = $"{database}_{timestamp}.dump";
    var filePath = Path.Combine("/app/backups", fileName);

    // Set password via environment variable (more secure than command line)
    var startInfo = new ProcessStartInfo
    {
        FileName = "pg_dump",
        Arguments = $"--host={host} --port={port} --username={username} " +
                   $"--dbname={database} --format=custom --file={filePath} " +
                   $"--verbose --no-password",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    // Set PGPASSWORD environment variable
    startInfo.EnvironmentVariables["PGPASSWORD"] = password;

    using var process = new Process { StartInfo = startInfo };
    
    var outputBuilder = new StringBuilder();
    var errorBuilder = new StringBuilder();

    process.OutputDataReceived += (sender, e) => 
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            outputBuilder.AppendLine(e.Data);
            _logger.LogDebug("pg_dump: {Output}", e.Data);
        }
    };

    process.ErrorDataReceived += (sender, e) => 
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            errorBuilder.AppendLine(e.Data);
            _logger.LogWarning("pg_dump stderr: {Error}", e.Data);
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        var errorMessage = errorBuilder.ToString();
        _logger.LogError("pg_dump failed with exit code {ExitCode}: {Error}", 
            process.ExitCode, errorMessage);
        return (false, string.Empty, 0);
    }

    var fileInfo = new FileInfo(filePath);
    if (!fileInfo.Exists)
    {
        _logger.LogError("Backup file was not created: {FilePath}", filePath);
        return (false, string.Empty, 0);
    }

    _logger.LogInformation("Backup completed successfully: {FilePath} ({Size} bytes)", 
        filePath, fileInfo.Length);

    return (true, filePath, fileInfo.Length);
}
```

### Ejemplo de Compresión Adicional

```csharp
public async Task<string> CompressBackupFile(string inputPath)
{
    var outputPath = $"{inputPath}.gz";

    var startInfo = new ProcessStartInfo
    {
        FileName = "gzip",
        Arguments = $"--best --keep {inputPath}",
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using var process = Process.Start(startInfo);
    await process.WaitForExitAsync();

    if (process.ExitCode == 0 && File.Exists(outputPath))
    {
        _logger.LogInformation("Backup compressed: {OutputPath}", outputPath);
        return outputPath;
    }

    return inputPath; // Return original if compression fails
}
```

## 🔐 Seguridad

### Variables de Entorno para Passwords

**✅ RECOMENDADO:**
```bash
export PGPASSWORD="secret_password"
pg_dump --host=... --username=... --dbname=... --no-password
```

**❌ NO RECOMENDADO:**
```bash
# Evitar password en línea de comandos (visible en ps/history)
pg_dump --host=... --username=... --password=secret_password
```

### Archivo .pgpass (Alternativa)

Crear archivo `~/.pgpass` con formato:
```
hostname:port:database:username:password
```

Permisos requeridos:
```bash
chmod 600 ~/.pgpass
```

## 📊 Opciones Avanzadas de pg_dump

### Excluir Tablas

```bash
pg_dump \
  --exclude-table=logs \
  --exclude-table=audit_* \
  database_name
```

### Solo Esquema (Sin Datos)

```bash
pg_dump --schema-only database_name > schema.sql
```

### Solo Datos (Sin Esquema)

```bash
pg_dump --data-only database_name > data.sql
```

### Backup con Timestamps

```bash
pg_dump \
  --dbname=mydb \
  --file="mydb_$(date +%Y%m%d_%H%M%S).dump" \
  --format=custom
```

## 🚀 Estrategia de Backup Recomendada

### Para Bases de Datos Pequeñas (<1 GB)

```bash
pg_dump --format=custom --compress=9 database.dump
```

### Para Bases de Datos Medianas (1-10 GB)

```bash
pg_dump --format=custom --compress=6 database.dump
```

### Para Bases de Datos Grandes (>10 GB)

```bash
pg_dump --format=directory --jobs=4 backup_directory
```

## 🧪 Verificación de Backups

### Listar Contenido del Backup

```bash
pg_restore --list backup.dump
```

### Validar Backup (Dry-run)

```bash
pg_restore --list backup.dump > /dev/null
echo $?  # 0 = válido, otro = error
```

## 📦 Estructura de Archivos en el Worker

```
/app/backups/
├── mydb_20241130_143022.dump        # Backup original
├── mydb_20241130_143022.dump.gz     # Backup comprimido
└── metadata.json                     # Metadata del backup
```

## 🔄 Workflow Completo en el Worker

```
1. Recibir mensaje de backup desde RabbitMQ
   ↓
2. Validar autorización (WorkerAuthorizationService)
   ↓
3. Ejecutar pg_dump
   • Formato: custom
   • Compresión: nivel 6
   • Output: /app/backups/{database}_{timestamp}.dump
   ↓
4. Verificar archivo creado
   ↓
5. (Opcional) Compresión adicional con gzip
   ↓
6. Subir a Azure Blob Storage
   ↓
7. Actualizar API con metadata
   ↓
8. Eliminar archivo temporal local
   ↓
9. ACK mensaje RabbitMQ
```

## 📚 Referencias

- [PostgreSQL pg_dump Documentation](https://www.postgresql.org/docs/current/app-pgdump.html)
- [PostgreSQL pg_restore Documentation](https://www.postgresql.org/docs/current/app-pgrestore.html)
- [Backup Best Practices](https://www.postgresql.org/docs/current/backup.html)

## ⚠️ Notas Importantes

1. **Versión Compatibility**: Usar pg_dump/pg_restore de versión igual o superior a la del servidor
2. **Locks**: pg_dump NO bloquea la base de datos (puede ejecutarse en producción)
3. **Espacio en Disco**: Asegurar espacio suficiente en `/app/backups` (2x tamaño de DB)
4. **Timeout**: Configurar timeouts apropiados para bases grandes
5. **Network**: Asegurar conectividad de red entre Worker y servidor PostgreSQL
