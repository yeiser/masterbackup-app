import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BackupStatistics } from '../models/backup-statistics.model';

@Injectable({
  providedIn: 'root'
})
export class BackupStatisticsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/backuphistory`;

  /**
   * Get backup statistics with optional filters
   * @param backupScheduleId Optional filter by backup schedule
   * @param databaseConnectionId Optional filter by database connection
   * @param startDate Optional start date for statistics
   * @param endDate Optional end date for statistics
   */
  getStatistics(
    backupScheduleId?: string,
    databaseConnectionId?: string,
    startDate?: Date,
    endDate?: Date
  ): Observable<BackupStatistics> {
    let params = new HttpParams();
    
    if (backupScheduleId) {
      params = params.set('backupScheduleId', backupScheduleId);
    }
    if (databaseConnectionId) {
      params = params.set('databaseConnectionId', databaseConnectionId);
    }
    if (startDate) {
      params = params.set('startDate', startDate.toISOString());
    }
    if (endDate) {
      params = params.set('endDate', endDate.toISOString());
    }

    return this.http.get<BackupStatistics>(`${this.apiUrl}/statistics`, { params });
  }

  /**
   * Get backup statistics for the current month
   */
  getCurrentMonthStatistics(): Observable<BackupStatistics> {
    const now = new Date();
    const startDate = new Date(now.getFullYear(), now.getMonth(), 1);
    const endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    
    return this.getStatistics(undefined, undefined, startDate, endDate);
  }

  /**
   * Get backup statistics for the last N days
   */
  getRecentStatistics(days: number = 30): Observable<BackupStatistics> {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - days);
    
    return this.getStatistics(undefined, undefined, startDate, endDate);
  }

  /**
   * Get overall backup statistics (no date filter)
   */
  getOverallStatistics(): Observable<BackupStatistics> {
    return this.getStatistics();
  }
}
