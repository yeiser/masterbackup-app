export enum WorkerStatus {
  Online = 'Online',
  Offline = 'Offline',
  Busy = 'Busy',
  Error = 'Error'
}

export interface WorkerDto {
  id: string;
  tenantId: string;
  name: string;
  description?: string;
  ipAddress?: string;
  macAddress?: string;
  hostname?: string;
  osInfo?: string;
  status: WorkerStatus;
  lastHeartbeat: string;
  lastJobExecution?: string;
  supportedDatabaseTypes: string[];
  maxConcurrentJobs: number;
  currentActiveJobs: number;
  version?: string;
  totalBackupsProcessed: number;
  totalBytesProcessed: number;
  isActive: boolean;
  tags: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface WorkerStatsDto {
  totalWorkers: number;
  onlineWorkers: number;
  offlineWorkers: number;
  busyWorkers: number;
  totalBackupsProcessed: number;
  totalBytesProcessed: number;
}

export const WorkerStatusLabels: { [key in WorkerStatus]: string } = {
  [WorkerStatus.Online]: 'En Línea',
  [WorkerStatus.Offline]: 'Fuera de Línea',
  [WorkerStatus.Busy]: 'Ocupado',
  [WorkerStatus.Error]: 'Error'
};

export const WorkerStatusColors: { [key in WorkerStatus]: string } = {
  [WorkerStatus.Online]: 'success',
  [WorkerStatus.Offline]: 'secondary',
  [WorkerStatus.Busy]: 'warning',
  [WorkerStatus.Error]: 'danger'
};

export const WorkerStatusIcons: { [key in WorkerStatus]: string } = {
  [WorkerStatus.Online]: 'fa-check-circle',
  [WorkerStatus.Offline]: 'fa-times-circle',
  [WorkerStatus.Busy]: 'fa-spinner',
  [WorkerStatus.Error]: 'fa-exclamation-triangle'
};
