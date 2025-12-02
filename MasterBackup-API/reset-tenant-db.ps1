# Script to reset a tenant database by dropping and recreating the schema
# This is useful when migrations fail and leave the database in an inconsistent state

param(
    [Parameter(Mandatory=$false)]
    [string]$TenantId = "ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b",
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = $env:MASTER_DATABASE_CONNECTION
)

# Validate connection string
if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Host "Error: No connection string provided." -ForegroundColor Red
    Write-Host "Either set MASTER_DATABASE_CONNECTION environment variable or provide -ConnectionString parameter"
    exit 1
}

Write-Host "=== Tenant Database Reset Script ===" -ForegroundColor Cyan
Write-Host "Tenant ID: $TenantId" -ForegroundColor Yellow
Write-Host ""

# Parse connection string to get components
$builder = New-Object Npgsql.NpgsqlConnectionStringBuilder($ConnectionString)
$host = $builder.Host
$port = $builder.Port
$masterDb = $builder.Database
$username = $builder.Username
$password = $builder.Password

# Build tenant database name (remove hyphens from GUID)
$dbName = "tenant_$($TenantId.Replace('-', ''))"

Write-Host "Target database: $dbName" -ForegroundColor Yellow
Write-Host "Host: $host:$port" -ForegroundColor Gray
Write-Host ""

# Confirm action
$confirmation = Read-Host "This will DROP and RECREATE the public schema in '$dbName'. Continue? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

try {
    Write-Host "Connecting to master database..." -ForegroundColor Cyan
    
    # Build connection string for tenant database
    $tenantConnStr = "Host=$host;Port=$port;Database=$dbName;Username=$username;Password=$password"
    
    # Connect and drop/recreate schema
    Write-Host "Dropping public schema..." -ForegroundColor Yellow
    
    $null = & psql -h $host -p $port -U $username -d $dbName -c "DROP SCHEMA IF EXISTS public CASCADE;"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to drop schema"
    }
    
    Write-Host "Creating public schema..." -ForegroundColor Yellow
    $null = & psql -h $host -p $port -U $username -d $dbName -c "CREATE SCHEMA public;"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create schema"
    }
    
    Write-Host "Granting permissions..." -ForegroundColor Yellow
    $null = & psql -h $host -p $port -U $username -d $dbName -c "GRANT ALL ON SCHEMA public TO $username;"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to grant permissions"
    }
    
    Write-Host ""
    Write-Host "✓ Successfully reset tenant database schema!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Clear the migration cache by calling: POST /api/System/clear-migrations-cache?tenantId=$TenantId" -ForegroundColor Gray
    Write-Host "2. Restart the API or make a request as this tenant to trigger auto-migration" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "✗ Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternative: Use this SQL directly in pgAdmin or psql:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "DROP SCHEMA IF EXISTS public CASCADE;" -ForegroundColor White
    Write-Host "CREATE SCHEMA public;" -ForegroundColor White
    Write-Host "GRANT ALL ON SCHEMA public TO $username;" -ForegroundColor White
    Write-Host ""
    exit 1
}
