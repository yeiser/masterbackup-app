# Configuración de Azure Blob Storage

## Cambios Implementados

Se ha configurado el servicio de Azure Blob Storage para usar variables de entorno, permitiendo mayor seguridad y flexibilidad en diferentes ambientes (desarrollo, staging, producción).

## Variable de Entorno

### `AZURE_STORAGE_CONNECTION_STRING`

**Descripción:** Connection string de Azure Storage Account para almacenar archivos de backup.

**Prioridad:** La variable de entorno tiene precedencia sobre `appsettings.json`.

**Valores según ambiente:**

#### Desarrollo Local
```bash
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
```
Usa el emulador de Azure Storage (Azurite).

#### Producción
```bash
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=<nombre_cuenta>;AccountKey=<clave_acceso>;EndpointSuffix=core.windows.net
```

## Cómo Obtener el Connection String de Azure

1. Inicia sesión en [Azure Portal](https://portal.azure.com)
2. Navega a tu Storage Account
3. En el menú lateral, selecciona **Access Keys**
4. Copia el **Connection string** de key1 o key2

## Configuración por Ambiente

### Desarrollo Local

1. Instala [Azurite](https://github.com/Azure/Azurite) (emulador de Azure Storage):
   ```bash
   npm install -g azurite
   ```

2. Inicia Azurite:
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. Configura la variable de entorno:
   ```bash
   # PowerShell
   $env:AZURE_STORAGE_CONNECTION_STRING="UseDevelopmentStorage=true"
   
   # CMD
   set AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
   ```

4. O crea un archivo `.env` en la raíz del proyecto API:
   ```env
   AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
   ```

### Docker

Agrega la variable de entorno en `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      AZURE_STORAGE_CONNECTION_STRING: ${AZURE_STORAGE_CONNECTION_STRING}
```

Y define el valor en un archivo `.env` en la raíz del proyecto:
```env
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
```

### Kubernetes

Crea un Secret:

```bash
kubectl create secret generic azure-storage-secret \
  --from-literal=connection-string='DefaultEndpointsProtocol=https;AccountName=...'
```

Referencia en el Deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: masterbackup-api
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: AZURE_STORAGE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: azure-storage-secret
              key: connection-string
```

### Azure App Service

1. Ve a tu App Service en Azure Portal
2. Navega a **Configuration > Application settings**
3. Haz clic en **New application setting**
4. Nombre: `AZURE_STORAGE_CONNECTION_STRING`
5. Valor: Tu connection string de Azure Storage
6. Marca como **Deployment slot setting** si usas slots
7. Guarda los cambios

## Estructura de Contenedores

El servicio crea un contenedor por tenant con el formato:

```
backups-{tenantId}
```

Ejemplo:
- Tenant ID: `550e8400-e29b-41d4-a716-446655440000`
- Contenedor: `backups-550e8400-e29b-41d4-a716-446655440000`

Puedes personalizar el prefijo en `appsettings.json`:

```json
{
  "AzureStorage": {
    "ContainerPrefix": "backups"
  }
}
```

## Código Modificado

### Program.cs

```csharp
// Get Azure Blob Storage connection string from environment variable or configuration
var azureStorageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                                    ?? builder.Configuration["AzureStorage:ConnectionString"]
                                    ?? throw new Exception("Azure Storage connection string not configured");

// Register service with dependency injection
builder.Services.AddSingleton<IBlobStorageService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AzureBlobStorageService>>();
    var containerPrefix = builder.Configuration["AzureStorage:ContainerPrefix"] ?? "backups";
    return new AzureBlobStorageService(azureStorageConnectionString, containerPrefix, logger);
});
```

### AzureBlobStorageService.cs

El constructor ahora acepta el connection string directamente:

```csharp
public AzureBlobStorageService(
    string connectionString,
    string containerPrefix,
    ILogger<AzureBlobStorageService> logger)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new ArgumentNullException(nameof(connectionString));
    }
    
    _blobServiceClient = new BlobServiceClient(connectionString);
    _containerPrefix = containerPrefix ?? "backups";
    _logger = logger;
}
```

## Verificación

Para verificar que la configuración es correcta, revisa los logs al iniciar la aplicación:

```
[INFO] Azure Storage configured: Development Emulator
```
o
```
[INFO] Azure Storage configured: Azure Cloud
```

## Seguridad

⚠️ **IMPORTANTE:**

- ✅ **NUNCA** incluyas el connection string de producción en el código fuente
- ✅ **NUNCA** commitees archivos `.env` al repositorio
- ✅ Usa Azure Key Vault en producción para mayor seguridad
- ✅ Rota las claves de acceso periódicamente
- ✅ Usa Managed Identity cuando sea posible en Azure

## Troubleshooting

### Error: "Azure Storage connection string not configured"

**Causa:** La variable de entorno no está definida y tampoco existe en appsettings.json.

**Solución:**
1. Define la variable de entorno `AZURE_STORAGE_CONNECTION_STRING`
2. O agrega el valor en `appsettings.json` bajo `AzureStorage:ConnectionString`

### Error: "No connection could be made"

**Causa:** El emulador Azurite no está corriendo (en desarrollo).

**Solución:**
```bash
azurite --silent --location c:\azurite
```

### Error: "Server failed to authenticate the request"

**Causa:** Connection string inválido o clave de acceso incorrecta.

**Solución:**
1. Verifica que el connection string sea correcto
2. Regenera las claves en Azure Portal si es necesario
3. Actualiza la variable de entorno con el nuevo valor

## Referencias

- [Azure Blob Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [Azurite (Storage Emulator)](https://github.com/Azure/Azurite)
- [Azure Storage .NET SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/storage)
