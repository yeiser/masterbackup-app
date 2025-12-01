-- Script para eliminar la tabla Worker y la foreign key del TenantDb
-- Ejecutar este script en CADA base de datos de tenant

-- 1. Eliminar la foreign key
ALTER TABLE "DatabaseConnections" 
DROP CONSTRAINT IF EXISTS "FK_DatabaseConnections_Worker_AssignedWorkerId";

-- 2. Eliminar el índice
DROP INDEX IF EXISTS "IX_DatabaseConnections_AssignedWorkerId";

-- 3. Eliminar la tabla Worker (si existe)
DROP TABLE IF EXISTS "Worker";

-- 4. Eliminar la tabla Tenant (si existe en el tenant - no debería estar aquí)
DROP TABLE IF EXISTS "Tenant";

-- 5. Insertar el registro de migración
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251201145833_RemoveWorkerTableFromTenant', '8.0.4')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verificar que se aplicó correctamente
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
