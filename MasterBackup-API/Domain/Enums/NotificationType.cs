namespace MasterBackup_API.Domain.Enums;

public enum NotificationType
{
    BackupCompleted,
    BackupFailed,
    BackupStarted,
    BackupCancelled,
    ScheduleCreated,
    ScheduleUpdated,
    ScheduleDeleted,
    DatabaseConnectionAdded,
    DatabaseConnectionFailed,
    System
}
