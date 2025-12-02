import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  BackupScheduleDto, 
  CreateBackupScheduleDto, 
  UpdateBackupScheduleDto 
} from '../models/backup-schedule.models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BackupScheduleService {
  private apiUrl = `${environment.apiUrl}/BackupSchedules`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener todos los schedules con filtros
   */
  getAllSchedules(
    databaseConnectionId?: string,
    isActive?: boolean,
    searchTerm?: string,
    page: number = 1,
    pageSize: number = 100,
    skipLoading: boolean = false
  ): Observable<BackupScheduleDto[]> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (databaseConnectionId) {
      params = params.set('databaseConnectionId', databaseConnectionId);
    }
    if (isActive !== undefined) {
      params = params.set('isActive', isActive.toString());
    }
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<BackupScheduleDto[]>(this.apiUrl, { params, headers });
  }

  /**
   * Obtener un schedule por ID
   */
  getScheduleById(id: string): Observable<BackupScheduleDto> {
    return this.http.get<BackupScheduleDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Crear nuevo schedule
   */
  createSchedule(dto: CreateBackupScheduleDto): Observable<BackupScheduleDto> {
    return this.http.post<BackupScheduleDto>(this.apiUrl, dto);
  }

  /**
   * Actualizar schedule existente
   */
  updateSchedule(id: string, dto: UpdateBackupScheduleDto): Observable<BackupScheduleDto> {
    return this.http.put<BackupScheduleDto>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Eliminar schedule
   */
  deleteSchedule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Pausar schedule (isActive = false)
   */
  pauseSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/pause`, {});
  }

  /**
   * Reanudar schedule (isActive = true)
   */
  resumeSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/resume`, {});
  }

  /**
   * Ejecutar schedule manualmente
   */
  executeNow(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/execute-now`, {});
  }
}
