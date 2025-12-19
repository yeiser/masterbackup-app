export interface PlanLimits {
  maxDatabases: number;
  maxUsers: number;
  maxStorageGB: number;
  backupRetentionDays: number;
  cloudStorageEnabled: boolean;
  scheduledBackupsEnabled: boolean;
  apiAccessEnabled: boolean;
  prioritySupport: boolean;
  customBrandingEnabled: boolean;
}

export interface CurrentUsage {
  databasesCount: number;
  usersCount: number;
  storageUsedGB: number;
  storagePercentage: number;
}

export interface SubscriptionInfo {
  id: string;
  planName: string;
  planDisplayName: string;
  status: string;
  endDate: string;
  daysRemaining: number;
  limits: PlanLimits;
  usage: CurrentUsage;
}
