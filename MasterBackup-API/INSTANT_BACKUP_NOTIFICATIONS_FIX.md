# Instant Backup Notifications Fix

## Problem
Email and in-app notifications were not being sent for **instant backups** (manual backups executed by users). The issue occurred because:

1. Instant backups have `BackupScheduleId = null` (they're not scheduled)
2. The notification logic checked `if (backupHistory.BackupSchedule == null)` and returned early
3. There was no way to identify which user initiated the instant backup

## Solution
Added support for tracking and notifying users who initiate instant backups by:

### 1. Database Schema Changes
- **Added `InitiatedBy` field** to `BackupHistory` entity to store the user ID who initiated the backup
- **Created migration**: `AddInitiatedByToBackupHistory`

**File**: `Domain/Entities/BackupHistory.cs`
```csharp
/// <summary>
/// User ID who initiated the backup (for instant backups)
/// </summary>
public string? InitiatedBy { get; set; }
```

### 2. Command Changes
- **Updated `ExecuteInstantBackupCommand`** to include `InitiatedBy` property

**File**: `Application/Features/Backups/Commands/ExecuteInstantBackupCommand.cs`
```csharp
public string? InitiatedBy { get; set; }
```

### 3. Controller Changes
- **Updated `BackupsController.ExecuteInstantBackup`** to extract current user ID from claims and pass it to the command

**File**: `Presentation/Controllers/BackupsController.cs`
```csharp
// Get current user ID from claims
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

var command = new ExecuteInstantBackupCommand
{
    // ... other properties
    InitiatedBy = userId
};
```

### 4. Handler Changes
- **Updated `ExecuteInstantBackupCommandHandler`** to store `InitiatedBy` when creating the `BackupHistory` record

**File**: `Application/Features/Backups/Commands/ExecuteInstantBackupCommandHandler.cs`
```csharp
var backupHistory = new BackupHistory
{
    // ... other properties
    IsInstantBackup = true,
    InitiatedBy = request.InitiatedBy,
    // ...
};
```

### 5. Notification Logic Changes
**Updated two methods in `UpdateBackupStatusCommandHandler`:**

#### A. `SendEmailNotificationAsync`
- Now handles instant backups separately from scheduled backups
- For instant backups:
  - Always sends notification (ignores schedule notification settings)
  - Uses `InitiatedBy` to identify the recipient
  - Uses "Instant Backup" as the schedule name
- For scheduled backups:
  - Continues to use existing logic with notification settings

#### B. `CreateBackupNotificationAsync`
- Now handles instant backups separately from scheduled backups
- For instant backups:
  - Always creates notification (ignores schedule notification settings)
  - Uses `InitiatedBy` to identify the recipient
  - Uses "Instant Backup" as the schedule name
- For scheduled backups:
  - Continues to use existing logic with notification settings

**File**: `Application/Features/Backups/Commands/UpdateBackupStatusCommandHandler.cs`

### Key Logic Pattern (Applied to Both Methods)
```csharp
string userId;
string scheduleName;
bool shouldNotify = true;

// Handle instant backups vs scheduled backups differently
if (backupHistory.BackupSchedule == null)
{
    // This is an instant backup
    if (string.IsNullOrEmpty(backupHistory.InitiatedBy))
    {
        _logger.LogWarning("Instant backup Job {JobId} has no InitiatedBy user, skipping notification", request.JobId);
        return;
    }

    // For instant backups, always notify the user who initiated it
    userId = backupHistory.InitiatedBy;
    scheduleName = "Instant Backup";
    shouldNotify = true;
}
else
{
    // This is a scheduled backup
    var schedule = backupHistory.BackupSchedule;

    // Apply notification rules based on schedule settings
    if (isSuccess)
    {
        shouldNotify = schedule.NotifyOnCompletion && !schedule.NotifyOnlyOnFailure;
    }
    else
    {
        shouldNotify = schedule.NotifyOnCompletion || schedule.NotifyOnlyOnFailure;
    }

    if (!shouldNotify)
    {
        _logger.LogDebug("Notification skipped based on schedule settings", request.JobId);
        return;
    }

    userId = schedule.CreatedBy.ToString();
    scheduleName = schedule.Name ?? "Unnamed Schedule";
}

// Rest of the notification logic uses userId and scheduleName
```

## Notification Rules Summary

### Instant Backups (Manual)
- **Email Notification**: ✅ Always sent to the user who initiated the backup
- **In-App Notification**: ✅ Always created for the user who initiated the backup
- **Notification Settings**: Ignored (instant backups always notify)
- **Recipient**: User who clicked "Execute Backup" (`InitiatedBy`)

### Scheduled Backups (Automated)
- **Email Notification**: Based on `BackupSchedule.NotifyOnCompletion` and `NotifyOnlyOnFailure` settings
- **In-App Notification**: Based on `BackupSchedule.NotifyOnCompletion` and `NotifyOnlyOnFailure` settings
- **Notification Settings**: 
  - Success: Notify if `NotifyOnCompletion = true` AND `NotifyOnlyOnFailure = false`
  - Failure: Notify if `NotifyOnCompletion = true` OR `NotifyOnlyOnFailure = true`
- **Recipient**: User who created the schedule (`BackupSchedule.CreatedBy`)

## Migration Required
To apply these changes to the database, run:
```bash
dotnet ef database update --context TenantDbContext
```

This will add the `InitiatedBy` column to the `BackupHistories` table.

## Testing Checklist
- [ ] Execute an instant backup as a user
- [ ] Verify email notification is received by the user who executed the backup
- [ ] Verify in-app notification appears for the user
- [ ] Verify notification shows "Instant Backup" as the backup name
- [ ] Verify scheduled backups still work with existing notification rules
- [ ] Verify notification settings (NotifyOnCompletion, NotifyOnlyOnFailure) still work for scheduled backups

## Additional Fixes Applied
- **Fixed Quartz.NET obsolete warning**: Removed `q.UseMicrosoftDependencyInjectionJobFactory()` as it's now the default behavior

## Files Modified
1. `Domain/Entities/BackupHistory.cs` - Added InitiatedBy property
2. `Application/Features/Backups/Commands/ExecuteInstantBackupCommand.cs` - Added InitiatedBy parameter
3. `Presentation/Controllers/BackupsController.cs` - Extract and pass user ID
4. `Application/Features/Backups/Commands/ExecuteInstantBackupCommandHandler.cs` - Store InitiatedBy
5. `Application/Features/Backups/Commands/UpdateBackupStatusCommandHandler.cs` - Handle instant backup notifications
6. `Program.cs` - Fixed Quartz.NET obsolete warning

## Migration Files Created
- `Infrastructure/Persistence/Migrations/Tenant/[Timestamp]_AddInitiatedByToBackupHistory.cs`
