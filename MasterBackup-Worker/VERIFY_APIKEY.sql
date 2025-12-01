-- Script para verificar y obtener ApiKey de un tenant

-- 1. Ver todos los tenants activos con sus ApiKeys
SELECT 
    Id as TenantId,
    TenantName,
    ApiKey,
    IsActive,
    CreatedAt
FROM Tenants
WHERE IsActive = 1
ORDER BY CreatedAt DESC;

-- 2. Verificar si el ApiKey actual del Worker existe
SELECT 
    Id as TenantId,
    TenantName,
    ApiKey,
    IsActive
FROM Tenants
WHERE ApiKey = 'mb_uJ2UKAAA';

-- 3. Si no hay tenants, necesitas crear uno primero registrándote en la aplicación web
-- o ejecutando este script (reemplaza los valores):

/*
DECLARE @TenantId UNIQUEIDENTIFIER = NEWID();
DECLARE @ApiKey NVARCHAR(50) = 'mb_' + LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 10);

INSERT INTO Tenants (Id, TenantName, ConnectionString, ApiKey, IsActive, CreatedAt, UpdatedAt)
VALUES (
    @TenantId,
    'Test Tenant',
    'Server=localhost;Database=MasterBackup_Tenant_' + CONVERT(NVARCHAR(36), @TenantId) + ';Trusted_Connection=True;TrustServerCertificate=True;',
    @ApiKey,
    1,
    GETUTCDATE(),
    GETUTCDATE()
);

SELECT 
    Id as TenantId,
    TenantName,
    ApiKey,
    ConnectionString
FROM Tenants
WHERE Id = @TenantId;
*/

-- 4. Copiar el ApiKey del resultado y actualizarlo en appsettings.json del Worker
