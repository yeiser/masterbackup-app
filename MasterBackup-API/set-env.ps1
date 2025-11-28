# Script para configurar variables de entorno para MasterBackup API
# Uso: .\set-env.ps1

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  MasterBackup API - Configuración de Variables de Entorno" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

# Pedir conexión de base de datos master
Write-Host "Configuración de Base de Datos Master:" -ForegroundColor Yellow
$host = Read-Host "  Host [localhost]"
if ([string]::IsNullOrWhiteSpace($host)) { $host = "localhost" }

$port = Read-Host "  Puerto [5432]"
if ([string]::IsNullOrWhiteSpace($port)) { $port = "5432" }

$database = Read-Host "  Database [master_saas]"
if ([string]::IsNullOrWhiteSpace($database)) { $database = "master_saas" }

$username = Read-Host "  Username [postgres]"
if ([string]::IsNullOrWhiteSpace($username)) { $username = "postgres" }

$password = Read-Host "  Password" -AsSecureString
$passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
)

# Construir connection string
$masterConnection = "Host=$host;Port=$port;Database=$database;Username=$username;Password=$passwordText;Include Error Detail=true"

Write-Host ""
Write-Host "¿Deseas usar una base de datos diferente para Serilog? (S/N) [N]: " -NoNewline -ForegroundColor Yellow
$useDifferentSerilog = Read-Host

$serilogConnection = $masterConnection

if ($useDifferentSerilog -eq "S" -or $useDifferentSerilog -eq "s") {
    Write-Host ""
    Write-Host "Configuración de Base de Datos Serilog:" -ForegroundColor Yellow
    $serilogHost = Read-Host "  Host [$host]"
    if ([string]::IsNullOrWhiteSpace($serilogHost)) { $serilogHost = $host }

    $serilogPort = Read-Host "  Puerto [$port]"
    if ([string]::IsNullOrWhiteSpace($serilogPort)) { $serilogPort = $port }

    $serilogDatabase = Read-Host "  Database [$database]"
    if ([string]::IsNullOrWhiteSpace($serilogDatabase)) { $serilogDatabase = $database }

    $serilogUsername = Read-Host "  Username [$username]"
    if ([string]::IsNullOrWhiteSpace($serilogUsername)) { $serilogUsername = $username }

    $serilogPassword = Read-Host "  Password" -AsSecureString
    $serilogPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($serilogPassword)
    )

    $serilogConnection = "Host=$serilogHost;Port=$serilogPort;Database=$serilogDatabase;Username=$serilogUsername;Password=$serilogPasswordText;Include Error Detail=true"
}

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "  Configuración Completa" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green
Write-Host ""

# Configurar variables de entorno
$env:MASTER_DATABASE_CONNECTION = $masterConnection
$env:MASTER_DATABASE_CONNECTION = $serilogConnection

Write-Host "✓ Variables de entorno configuradas para esta sesión" -ForegroundColor Green
Write-Host ""
Write-Host "MASTER_DATABASE_CONNECTION:" -ForegroundColor Cyan
Write-Host "  Host=$host;Port=$port;Database=$database;Username=$username;Password=***" -ForegroundColor Gray
Write-Host ""
Write-Host "MASTER_DATABASE_CONNECTION:" -ForegroundColor Cyan
if ($useDifferentSerilog -eq "S" -or $useDifferentSerilog -eq "s") {
    Write-Host "  Host=$serilogHost;Port=$serilogPort;Database=$serilogDatabase;Username=$serilogUsername;Password=***" -ForegroundColor Gray
} else {
    Write-Host "  (Usando la misma configuración que Master Database)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "  Para ejecutar la API:" -ForegroundColor Yellow
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "  Nota: Estas variables solo están disponibles en esta sesión" -ForegroundColor Gray
Write-Host "        Para persistencia, considera usar un archivo .env" -ForegroundColor Gray
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host ""

# Opción para guardar en archivo .env
Write-Host "¿Deseas guardar en archivo .env? (S/N) [N]: " -NoNewline -ForegroundColor Yellow
$saveToFile = Read-Host

if ($saveToFile -eq "S" -or $saveToFile -eq "s") {
    $envContent = @"
# MasterBackup API - Environment Variables
# Generado el: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

# Master Database Connection
MASTER_DATABASE_CONNECTION=$masterConnection

# Serilog Database Connection
MASTER_DATABASE_CONNECTION=$serilogConnection
"@

    $envContent | Out-File -FilePath ".env" -Encoding UTF8
    Write-Host ""
    Write-Host "✓ Archivo .env creado exitosamente" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠ IMPORTANTE: No commits este archivo al repositorio" -ForegroundColor Red
    Write-Host ""
}
