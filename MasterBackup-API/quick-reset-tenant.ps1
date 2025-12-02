# Quick script to reset tenant database schema
$masterConn = $env:MASTER_DATABASE_CONNECTION
if ([string]::IsNullOrEmpty($masterConn)) {
    Write-Host "Error: MASTER_DATABASE_CONNECTION not set" -ForegroundColor Red
    exit 1
}

$builder = New-Object Npgsql.NpgsqlConnectionStringBuilder($masterConn)
$tenantId = "ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b"
$dbName = "tenant_$($tenantId.Replace('-', ''))"
$builder.Database = $dbName
$tenantConn = $builder.ConnectionString

Write-Host "`nResetting tenant database: $dbName" -ForegroundColor Cyan

$conn = New-Object Npgsql.NpgsqlConnection($tenantConn)
try {
    $conn.Open()
    Write-Host "Connected!" -ForegroundColor Green
    
    $cmd = $conn.CreateCommand()
    
    Write-Host "Dropping public schema..." -ForegroundColor Yellow
    $cmd.CommandText = "DROP SCHEMA IF EXISTS public CASCADE;"
    $null = $cmd.ExecuteNonQuery()
    
    Write-Host "Creating public schema..." -ForegroundColor Yellow
    $cmd.CommandText = "CREATE SCHEMA public;"
    $null = $cmd.ExecuteNonQuery()
    
    Write-Host "Granting permissions..." -ForegroundColor Yellow
    $cmd.CommandText = "GRANT ALL ON SCHEMA public TO $($builder.Username);"
    $null = $cmd.ExecuteNonQuery()
    
    Write-Host "`n✓ Schema reset complete!" -ForegroundColor Green
    Write-Host "`nThe API will automatically apply migrations on next request." -ForegroundColor Cyan
} catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    if ($conn.State -eq 'Open') {
        $conn.Close()
    }
    $conn.Dispose()
}
