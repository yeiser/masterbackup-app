export interface BackupStatistics {
  totalBackups: number;
  successfulBackups: number;
  failedBackups: number;
  inProgressBackups: number;
  successRate: number;
  totalBackupSizeBytes: number;
  totalBackupSizeMB: number;
  totalBackupSizeGB: number;
  averageDuration: string | null;
  minDuration: string | null;
  maxDuration: string | null;
  lastSuccessfulBackup: Date | null;
  lastFailedBackup: Date | null;
  statusBreakdown: StatusBreakdown[];
  dailyBackupCounts: DailyBackupCount[];
}

export interface StatusBreakdown {
  status: BackupStatus;
  statusText: string;
  count: number;
  percentage: number;
}

export interface DailyBackupCount {
  date: Date;
  totalCount: number;
  successCount: number;
  failedCount: number;
}

export enum BackupStatus {
  Pending = 1,
  InProgress = 2,
  Completed = 3,
  Failed = 4,
  Cancelled = 5,
  Timeout = 6
}
