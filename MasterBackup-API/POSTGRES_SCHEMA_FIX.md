# PostgreSQL Schema Fix - Migration Error Resolution

## Problem
When attempting to apply tenant database migrations, PostgreSQL was throwing the error:
```
3F000: no schema has been selected to create in
POSITION: 14
```

This occurred because the tenant database connection strings didn't specify a `SearchPath` parameter, which tells PostgreSQL which schema to use for creating objects.

## Root Cause
1. **Missing SearchPath Parameter**: PostgreSQL requires the `SearchPath` parameter in connection strings to specify which schema to use. Without it, migrations fail because PostgreSQL doesn't know where to create tables.

2. **Previous Migration Failures**: The tenant database (tenant_ffa0a0030206433bb0fd0a2ef0ae0f2b) had partially failed migrations, leaving it in an inconsistent state.

## Solution Implemented

### Code Changes

#### 1. TenantService.cs - Line 79
**Added SearchPath when creating tenant databases:**
```csharp
// Build connection string for new tenant database
// Extract components from master connection string
var builder = new Npgsql.NpgsqlConnectionStringBuilder(masterConnectionString);
builder.Database = dbName;
builder.SearchPath = "public"; // ✓ ADDED: Ensure schema is set for PostgreSQL
var tenantConnectionString = builder.ConnectionString;
```

#### 2. TenantMiddleware.cs - Lines 83-89
**Ensure SearchPath is present before applying migrations:**
```csharp
try
{
    // Ensure connection string has SearchPath set for PostgreSQL
    var connectionString = EnsureSearchPath(tenantContext.ConnectionString);
    
    // Create TenantDbContext manually with the current tenant context
    var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
    optionsBuilder.UseNpgsql(connectionString);
```

#### 3. TenantMiddleware.cs - Lines 151-169
**Added helper method to ensure SearchPath:**
```csharp
/// <summary>
/// Ensure the connection string has SearchPath=public set for PostgreSQL.
/// This prevents "no schema has been selected to create in" errors during migrations.
/// </summary>
private static string EnsureSearchPath(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return connectionString;

    var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    
    // Only set SearchPath if not already set
    if (string.IsNullOrEmpty(builder.SearchPath))
    {
        builder.SearchPath = "public";
    }
    
    return builder.ConnectionString;
}
```

## Required Manual Steps

### Step 1: Reset Tenant Database Schema

The tenant database `tenant_ffa0a0030206433bb0fd0a2ef0ae0f2b` needs to be reset because previous migration attempts left it in an inconsistent state.

**Option A: Use pgAdmin or PostgreSQL GUI Tool**
1. Connect to database: `tenant_ffa0a0030206433bb0fd0a2ef0ae0f2b`
2. Execute the SQL script: `reset-tenant-schema.sql`

**Option B: Use psql Command Line**
```powershell
# Set environment variable if not already set
$env:PGPASSWORD = "your_password"

# Connect and execute
psql -h 54.39.107.101 -p 5432 -U siscolsi -d tenant_ffa0a0030206433bb0fd0a2ef0ae0f2b -f reset-tenant-schema.sql
```

**Option C: Manual SQL Execution**
Connect to the tenant database and run:
```sql
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO siscolsi;
GRANT ALL ON SCHEMA public TO PUBLIC;
```

### Step 2: Clear Migration Cache

After resetting the database, clear the API's migration cache:

**Option A: Restart the API**
Simply restart the API process - the cache is in-memory and will be cleared.

**Option B: Call Admin Endpoint**
```http
POST http://localhost:5241/api/System/clear-migrations-cache?tenantId=ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
Authorization: Bearer {admin_jwt_token}
```

### Step 3: Verify Auto-Migration

1. Make any authenticated request as the tenant user
2. Watch the API logs for:
   ```
   [WRN] Applying 7 pending migration(s) to tenant ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b: ...
   [INF] Successfully applied 7 migration(s) to tenant ffa0a003-0206-433b-b0fd-0a2ef0ae0f2b
   ```

3. Verify tables exist:
   ```sql
   SELECT table_name 
   FROM information_schema.tables 
   WHERE table_schema = 'public' 
   ORDER BY table_name;
   ```

Expected tables:
- `__EFMigrationsHistory`
- `BackupHistories`
- `BackupSchedules`
- `DatabaseConnections`

## Impact on Future Tenants

✅ **Good News**: All NEW tenant databases created via registration will automatically include `SearchPath=public` in their connection strings.

⚠️ **Existing Tenants**: If there are other existing tenants with the same issue, they would need the same fix applied:
1. Update their connection string in the `Tenants` table to include `;SearchPath=public`
2. Reset their database schema if migrations had already failed
3. Clear migration cache and let auto-migration run

## Testing Checklist

- [ ] Reset tenant database schema (Step 1)
- [ ] Clear migration cache (Step 2)
- [ ] Restart API or make request as tenant
- [ ] Verify migrations applied successfully in logs
- [ ] Verify tables created in database
- [ ] Test BackupSchedules API endpoints
- [ ] Test Angular BackupSchedule component
- [ ] Create a new test tenant to verify SearchPath is included

## Files Modified

1. `Infrastructure/Services/TenantService.cs` - Added SearchPath to new tenant databases
2. `Infrastructure/Middleware/TenantMiddleware.cs` - Added SearchPath validation and helper method
3. Created `reset-tenant-schema.sql` - SQL script for manual database reset
4. Created `quick-reset-tenant.ps1` - PowerShell script (requires Npgsql assembly loaded)
5. Created `reset-tenant-db.ps1` - Alternative PowerShell script with psql dependency

## Related Issues Fixed Previously

- PostgreSQL array syntax errors in migrations (`new[]` → `new string[]`)
- PostgreSQL filter syntax errors (`[column]` → `"column"`, `= 1` → `= true`)

## References

- PostgreSQL Documentation: [Schema Search Path](https://www.postgresql.org/docs/current/ddl-schemas.html#DDL-SCHEMAS-PATH)
- Npgsql Documentation: [Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters.html#schema-search-path)
- Entity Framework Core: [Npgsql Provider](https://www.npgsql.org/efcore/)
