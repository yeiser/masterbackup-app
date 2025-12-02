export enum BackupStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export interface BackupHistoryDto {
  id: string;
  jobId: string;
  databaseConnectionId: string;
  databaseConnectionName: string;
  backupScheduleId?: string;
  backupScheduleName?: string;
  status: BackupStatus;
  statusText: string;
  startTime: Date;
  endTime?: Date;
  duration?: string;
  blobUrl?: string;
  blobName?: string;
  backupSizeBytes?: number;
  backupSizeMB?: number;
  backupSizeGB?: number;
  errorMessage?: string;
  errorCode?: string;
  retryCount: number;
  compressionType?: string;
  isInstantBackup: boolean;
  createdAt: Date;
}

export interface GetBackupHistoryResult {
  items: BackupHistoryDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface BackupStatisticsDto {
  totalBackups: number;
  successfulBackups: number;
  failedBackups: number;
  inProgressBackups: number;
  successRate: number;
  totalBackupSizeBytes: number;
  totalBackupSizeMB: number;
  totalBackupSizeGB: number;
  averageDuration?: string;
  minDuration?: string;
  maxDuration?: string;
  lastSuccessfulBackup?: Date;
  lastFailedBackup?: Date;
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

export interface BackupHistoryByDatabase {
  databaseConnectionId: string;
  databaseConnectionName: string;
  totalBackups: number;
  successfulBackups: number;
  failedBackups: number;
  lastBackup: BackupHistoryDto;
  totalSizeGB: number;
  backups: BackupHistoryDto[];
}

export interface BackupHistoryFilters {
  pageNumber?: number;
  pageSize?: number;
  backupScheduleId?: string;
  databaseConnectionId?: string;
  status?: BackupStatus;
  isInstantBackup?: boolean;
  startDateFrom?: Date;
  startDateTo?: Date;
  sortBy?: string;
  sortDirection?: 'ASC' | 'DESC';
}
