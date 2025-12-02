# Script para obtener información del Tenant desde la base de datos
# Ejecutar desde la carpeta del Worker

Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Obtener información del Tenant para Worker           ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Leer la connection string de la variable de entorno o pedir al usuario
$connectionString = $env:MASTER_DATABASE_CONNECTION

if ([string]::IsNullOrEmpty($connectionString)) {
    Write-Host "La variable de entorno MASTER_DATABASE_CONNECTION no está configurada." -ForegroundColor Yellow
    Write-Host "Por favor ingresa la connection string de la base de datos Master:" -ForegroundColor Yellow
    $connectionString = Read-Host "Connection String"
}

Write-Host ""
Write-Host "Conectando a la base de datos..." -ForegroundColor Cyan

# Query SQL para obtener información de tenants
$query = @"
SELECT 
    "Id" as TenantId,
    "Name" as TenantName,
    "ApiKey",
    "IsActive",
    "CreatedAt"
FROM "Tenants"
WHERE "IsActive" = true
ORDER BY "CreatedAt" DESC;
"@

try {
    # Instalar el módulo Npgsql si no está instalado
    if (-not (Get-Module -ListAvailable -Name "Npgsql")) {
        Write-Host "Instalando módulo Npgsql..." -ForegroundColor Yellow
        Install-Module -Name Npgsql -Force -Scope CurrentUser
    }

    # Importar el módulo
    Import-Module Npgsql

    # Ejecutar query
    $connection = New-Object Npgsql.NpgsqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    
    $reader = $command.ExecuteReader()
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "Tenants disponibles:" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    
    $tenantCount = 0
    while ($reader.Read()) {
        $tenantCount++
        Write-Host "Tenant #$tenantCount" -ForegroundColor Cyan
        Write-Host "  TenantId:   $($reader['TenantId'])" -ForegroundColor White
        Write-Host "  Name:       $($reader['TenantName'])" -ForegroundColor White
        Write-Host "  ApiKey:     $($reader['ApiKey'])" -ForegroundColor White
        Write-Host "  Active:     $($reader['IsActive'])" -ForegroundColor White
        Write-Host "  Created:    $($reader['CreatedAt'])" -ForegroundColor White
        Write-Host ""
    }
    
    $reader.Close()
    $connection.Close()
    
    if ($tenantCount -eq 0) {
        Write-Host "No se encontraron tenants activos." -ForegroundColor Yellow
    } else {
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host "Para configurar el Worker, actualiza appsettings.json con:" -ForegroundColor Yellow
        Write-Host '  "Worker": {' -ForegroundColor White
        Write-Host '    "TenantId": "COPIA_EL_TENANTID_DE_ARRIBA",' -ForegroundColor White
        Write-Host '    "ApiKey": "COPIA_EL_APIKEY_DE_ARRIBA",' -ForegroundColor White
        Write-Host '    ...' -ForegroundColor White
        Write-Host '  }' -ForegroundColor White
        Write-Host ""
    }
    
} catch {
    Write-Host "Error al conectar a la base de datos:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Si Npgsql no está disponible, ejecuta este query manualmente en PostgreSQL:" -ForegroundColor Yellow
    Write-Host $query -ForegroundColor White
}

Write-Host ""
Write-Host "Presiona cualquier tecla para salir..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
