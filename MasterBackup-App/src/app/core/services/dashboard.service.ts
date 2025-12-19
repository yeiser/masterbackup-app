import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BackupStats } from '../models/backup-stats.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  getBackupStats(): Observable<BackupStats> {
    return this.http.get<BackupStats>(`${this.apiUrl}/dashboard/backup-stats`);
  }
}
