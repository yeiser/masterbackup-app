export interface BackupScheduleDto {
  id: string;
  tenantId: string;
  databaseConnectionId: string;
  databaseConnectionName: string;
  databaseName: string;
  databaseType: DatabaseType;
  name: string;
  description?: string;
  scheduleType: ScheduleType;
  cronExpression?: string;
  intervalMinutes?: number;
  nextRunTime?: Date;
  lastRunTime?: Date;
  retentionDays: number;
  isActive: boolean;
  timeZone?: string;
  maxRetries?: number;
  timeoutMinutes?: number;
  priority?: number;
  notifyOnCompletion?: boolean;
  notifyOnlyOnFailure?: boolean;
  createdAt: Date;
  updatedAt?: Date;
  createdBy: string;
  updatedBy?: string;
  backupHistoryCount: number;
  lastBackupStatus?: BackupStatus;
  lastBackupTime?: Date;
}

export interface CreateBackupScheduleDto {
  databaseConnectionId: string;
  name: string;
  description?: string;
  scheduleType: ScheduleType;
  cronExpression?: string;
  intervalMinutes?: number;
  retentionDays: number;
  isActive: boolean;
  timeZone?: string;
  maxRetries?: number;
  timeoutMinutes?: number;
  priority?: number;
  notifyOnCompletion?: boolean;
  notifyOnlyOnFailure?: boolean;
}

export interface UpdateBackupScheduleDto {
  name?: string;
  description?: string;
  scheduleType?: ScheduleType;
  cronExpression?: string;
  intervalMinutes?: number;
  retentionDays?: number;
  isActive?: boolean;
  timeZone?: string;
  maxRetries?: number;
  timeoutMinutes?: number;
  priority?: number;
  notifyOnCompletion?: boolean;
  notifyOnlyOnFailure?: boolean;
}

export enum ScheduleType {
  Cron = 1,
  Interval = 2
}

export enum DatabaseType {
  PostgreSQL = 1,
  MySQL = 2,
  SQLServer = 3,
  MongoDB = 4,
  MariaDB = 5
}

export enum BackupStatus {
  Pending = 1,
  InProgress = 2,
  Completed = 3,
  Failed = 4,
  Cancelled = 5,
  Timeout = 6
}

export const ScheduleTypeLabels: Record<ScheduleType, string> = {
  [ScheduleType.Cron]: 'Cron',
  [ScheduleType.Interval]: 'Intervalo'
};

export const ScheduleTypeIcons: Record<ScheduleType, string> = {
  [ScheduleType.Cron]: 'fa-clock',
  [ScheduleType.Interval]: 'fa-hourglass-half'
};

export const DatabaseTypeLabels: Record<DatabaseType, string> = {
  [DatabaseType.PostgreSQL]: 'PostgreSQL',
  [DatabaseType.MySQL]: 'MySQL',
  [DatabaseType.SQLServer]: 'SQL Server',
  [DatabaseType.MongoDB]: 'MongoDB',
  [DatabaseType.MariaDB]: 'MariaDB'
};

export const DatabaseTypeIcons: Record<DatabaseType, string> = {
  [DatabaseType.PostgreSQL]: 'fa-elephant',
  [DatabaseType.MySQL]: 'fa-dolphin',
  [DatabaseType.SQLServer]: 'fa-server',
  [DatabaseType.MongoDB]: 'fa-leaf',
  [DatabaseType.MariaDB]: 'fa-database'
};

export const BackupStatusLabels: Record<BackupStatus, string> = {
  [BackupStatus.Pending]: 'Pendiente',
  [BackupStatus.InProgress]: 'En Progreso',
  [BackupStatus.Completed]: 'Completado',
  [BackupStatus.Failed]: 'Fallido',
  [BackupStatus.Cancelled]: 'Cancelado',
  [BackupStatus.Timeout]: 'Timeout'
};

export const BackupStatusColors: Record<BackupStatus, string> = {
  [BackupStatus.Pending]: 'warning',
  [BackupStatus.InProgress]: 'info',
  [BackupStatus.Completed]: 'success',
  [BackupStatus.Failed]: 'danger',
  [BackupStatus.Cancelled]: 'secondary',
  [BackupStatus.Timeout]: 'dark'
};
