export enum DatabaseType {
  PostgreSQL = 1,
  MySQL = 2,
  SQLServer = 3,
  MongoDB = 4,
  MariaDB = 5
}

export enum WorkerAssignmentMode {
  Auto = 1,
  Dedicated = 2
}

export const WorkerAssignmentModeLabels: { [key in WorkerAssignmentMode]: string } = {
  [WorkerAssignmentMode.Auto]: 'Automático',
  [WorkerAssignmentMode.Dedicated]: 'Dedicado'
};

export interface DatabaseConnectionDto {
  id: string;
  name: string;
  description?: string;
  type: DatabaseType | string; // Backend envía como string
  host: string;
  port: number;
  database: string;
  username: string;
  engineVersion?: string;
  sslMode?: string;
  isActive: boolean;
  lastTestedAt?: string;
  lastTestSuccessful?: boolean;
  lastTestStatus?: string;
  assignedWorkerId?: string;
  tags?: string[];
  assignmentMode?: WorkerAssignmentMode | string; // Backend envía como string
  createdBy: string;
  createdByName: string;
  createdAt: string;
  updatedAt?: string;
  backupSchedulesCount: number;
}

export interface CreateDatabaseConnectionDto {
  name: string;
  description?: string;
  type: DatabaseType;
  host: string;
  port: number;
  database: string;
  username: string;
  password: string;
  engineVersion?: string;
  sslMode?: string;
  isActive: boolean;
  assignedWorkerId?: string;
  tags?: string[];
  assignmentMode?: WorkerAssignmentMode;
}

export interface UpdateDatabaseConnectionDto {
  name: string;
  description?: string;
  type: DatabaseType;
  host: string;
  port: number;
  database: string;
  username: string;
  password?: string;
  engineVersion?: string;
  sslMode?: string;
  isActive: boolean;
  assignedWorkerId?: string;
  tags?: string[];
  assignmentMode?: WorkerAssignmentMode;
}

export interface TestConnectionResultDto {
  success: boolean;
  message: string;
  responseTime?: number;
  serverVersion?: string;
  serverInfo?: string;
}

export const DatabaseTypeLabels: { [key in DatabaseType]: string } = {
  [DatabaseType.PostgreSQL]: 'PostgreSQL',
  [DatabaseType.MySQL]: 'MySQL',
  [DatabaseType.SQLServer]: 'SQL Server',
  [DatabaseType.MongoDB]: 'MongoDB',
  [DatabaseType.MariaDB]: 'MariaDB'
};

export const DatabaseTypeIcons: { [key in DatabaseType]: string } = {
  [DatabaseType.PostgreSQL]: 'fa-database text-primary',
  [DatabaseType.MySQL]: 'fa-database text-info',
  [DatabaseType.SQLServer]: 'fa-database text-warning',
  [DatabaseType.MongoDB]: 'fa-leaf text-success',
  [DatabaseType.MariaDB]: 'fa-database text-danger'
};
