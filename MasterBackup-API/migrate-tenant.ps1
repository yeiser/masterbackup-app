# Script para aplicar migraciones a la base de datos de un tenant específico
# Uso: .\migrate-tenant.ps1 -TenantDatabase "tenant_62fd92c0"

param(
    [Parameter(Mandatory=$true)]
    [string]$TenantDatabase
)

$Host = "54.39.107.101"
$Port = "5432"
$Username = "siscolsi"
$Password = "6jl0k1+RpJZo"

$ConnectionString = "Host=$Host;Port=$Port;Database=$TenantDatabase;Username=$Username;Password=$Password"

Write-Host "Aplicando migraciones a la base de datos: $TenantDatabase" -ForegroundColor Green

$env:TENANT_TEMPLATE_CONNECTION = $ConnectionString

dotnet ef database update --context TenantDbContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migraciones aplicadas exitosamente!" -ForegroundColor Green
} else {
    Write-Host "Error al aplicar migraciones" -ForegroundColor Red
}
