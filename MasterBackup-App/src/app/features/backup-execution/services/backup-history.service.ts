import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  BackupHistoryDto, 
  BackupHistoryFilters, 
  BackupStatisticsDto, 
  GetBackupHistoryResult,
  BackupHistoryByDatabase
} from '../models/backup-history.models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BackupHistoryService {
  private apiUrl = `${environment.apiUrl}/BackupHistory`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener historial de backups con filtros y paginación
   */
  getBackupHistory(
    filters?: BackupHistoryFilters,
    skipLoading: boolean = false
  ): Observable<GetBackupHistoryResult> {
    let params = new HttpParams();
    
    if (filters) {
      if (filters.pageNumber) params = params.set('pageNumber', filters.pageNumber.toString());
      if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());
      if (filters.backupScheduleId) params = params.set('backupScheduleId', filters.backupScheduleId);
      if (filters.databaseConnectionId) params = params.set('databaseConnectionId', filters.databaseConnectionId);
      if (filters.status) params = params.set('status', filters.status);
      if (filters.isInstantBackup !== undefined) params = params.set('isInstantBackup', filters.isInstantBackup.toString());
      if (filters.startDateFrom) params = params.set('startDateFrom', filters.startDateFrom.toISOString());
      if (filters.startDateTo) params = params.set('startDateTo', filters.startDateTo.toISOString());
      if (filters.sortBy) params = params.set('sortBy', filters.sortBy);
      if (filters.sortDirection) params = params.set('sortDirection', filters.sortDirection);
    }

    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<GetBackupHistoryResult>(this.apiUrl, { params, headers });
  }

  /**
   * Obtener un backup history por ID
   */
  getBackupHistoryById(id: string, skipLoading: boolean = false): Observable<BackupHistoryDto> {
    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<BackupHistoryDto>(`${this.apiUrl}/${id}`, { headers });
  }

  /**
   * Obtener estadísticas de backups
   */
  getBackupStatistics(
    backupScheduleId?: string,
    databaseConnectionId?: string,
    startDate?: Date,
    endDate?: Date,
    skipLoading: boolean = false
  ): Observable<BackupStatisticsDto> {
    let params = new HttpParams();
    
    if (backupScheduleId) params = params.set('backupScheduleId', backupScheduleId);
    if (databaseConnectionId) params = params.set('databaseConnectionId', databaseConnectionId);
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<BackupStatisticsDto>(`${this.apiUrl}/statistics`, { params, headers });
  }

  /**
   * Reintentar un backup fallido
   */
  retryBackup(id: string): Observable<{ message: string; originalBackupId: string; newJobId: string }> {
    return this.http.post<{ message: string; originalBackupId: string; newJobId: string }>(
      `${this.apiUrl}/${id}/retry`, 
      {}
    );
  }

  /**
   * Eliminar un backup history
   */
  deleteBackupHistory(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  /**
   * Eliminar múltiples backup histories
   */
  bulkDeleteBackupHistory(ids: string[]): Observable<{ message: string; deletedCount: number; totalRequested: number }> {
    return this.http.post<{ message: string; deletedCount: number; totalRequested: number }>(
      `${this.apiUrl}/bulk-delete`,
      ids
    );
  }

  /**
   * Obtener URL de descarga de un backup
   */
  getDownloadUrl(id: string): Observable<{
    blobUrl: string;
    blobName: string;
    backupSizeBytes?: number;
    backupSizeMB?: number;
    message: string;
  }> {
    return this.http.get<{
      blobUrl: string;
      blobName: string;
      backupSizeBytes?: number;
      backupSizeMB?: number;
      message: string;
    }>(`${this.apiUrl}/${id}/download`);
  }

  /**
   * Obtener backups agrupados por base de datos
   */
  getBackupHistoryByDatabase(
    startDate?: Date,
    endDate?: Date,
    skipLoading: boolean = false
  ): Observable<BackupHistoryByDatabase[]> {
    let params = new HttpParams();
    
    if (startDate) params = params.set('startDate', startDate.toISOString());
    if (endDate) params = params.set('endDate', endDate.toISOString());

    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<BackupHistoryByDatabase[]>(`${this.apiUrl}/by-database`, { params, headers });
  }
}
