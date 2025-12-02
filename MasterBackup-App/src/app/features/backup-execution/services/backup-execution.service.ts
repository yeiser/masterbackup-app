import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ExecuteInstantBackupDto {
  databaseConnectionId: string;
  compressionType?: string;
  timeoutMinutes?: number;
  maxRetries?: number;
}

export interface ExecuteInstantBackupResult {
  backupHistoryId: string;
  jobId: string;
  message: string;
  queuedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class BackupExecutionService {
  private apiUrl = `${environment.apiUrl}/Backups`;

  constructor(private http: HttpClient) {}

  /**
   * Ejecutar un backup instantáneo
   */
  executeInstantBackup(dto: ExecuteInstantBackupDto): Observable<ExecuteInstantBackupResult> {
    return this.http.post<ExecuteInstantBackupResult>(`${this.apiUrl}/execute-instant`, dto);
  }
}
