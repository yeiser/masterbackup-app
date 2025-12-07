-- Migration: Make JobId unique in BackupHistory table
-- Purpose: Prevent duplicate BackupHistory records for the same backup job
-- Date: December 5, 2024

-- Step 1: Check for existing duplicates before applying unique constraint
SELECT "JobId", COUNT(*) as count
FROM "BackupHistories"
GROUP BY "JobId"
HAVING COUNT(*) > 1;

-- Step 2: If duplicates exist, keep only the most recent record
-- (Run this only if step 1 returns results)
WITH duplicates AS (
    SELECT "Id", 
           "JobId",
           "CreatedAt",
           ROW_NUMBER() OVER (PARTITION BY "JobId" ORDER BY "CreatedAt" DESC, "UpdatedAt" DESC) as rn
    FROM "BackupHistories"
)
DELETE FROM "BackupHistories"
WHERE "Id" IN (
    SELECT "Id" FROM duplicates WHERE rn > 1
);

-- Step 3: Create unique index on JobId
DROP INDEX IF EXISTS "IX_BackupHistories_JobId";
CREATE UNIQUE INDEX "IX_BackupHistories_JobId" ON "BackupHistories" ("JobId");

-- Verification: This should now fail if you try to insert a duplicate JobId
-- INSERT INTO "BackupHistories" ("Id", "JobId", "DatabaseConnectionId", "Status", "StartTime", "CreatedAt", "UpdatedAt") 
-- VALUES (gen_random_uuid(), (SELECT "JobId" FROM "BackupHistories" LIMIT 1), (SELECT "DatabaseConnectionId" FROM "BackupHistories" LIMIT 1), 0, NOW(), NOW(), NOW());
