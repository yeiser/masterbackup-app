# Script para probar el logging del API
Write-Host "Esperando que el API esté listo..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Hacer algunas peticiones para generar logs
Write-Host "Haciendo petición al API para generar logs..." -ForegroundColor Cyan

try {
    # Petición inválida para generar error log
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body '{"email":"test@test.com","password":"wrongpassword"}' `
        -ErrorAction SilentlyContinue
} catch {
    Write-Host "Petición completada (error esperado)" -ForegroundColor Green
}

Write-Host "`nLogs deberían estar guardándose en la base de datos." -ForegroundColor Green
Write-Host "Puedes verificar con esta consulta SQL:" -ForegroundColor Yellow
Write-Host 'SELECT "Id", "Message", "Level", "TimeStamp", "Exception" FROM "Logs" ORDER BY "TimeStamp" DESC LIMIT 10;' -ForegroundColor White
