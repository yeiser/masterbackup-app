import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkerDto, WorkerStatsDto } from '../models/worker.models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class WorkerService {
  private apiUrl = `${environment.apiUrl}/workers`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener todos los workers del tenant
   */
  getAllWorkers(): Observable<WorkerDto[]> {
    return this.http.get<WorkerDto[]>(this.apiUrl);
  }

  /**
   * Obtener un worker por ID
   */
  getWorkerById(id: string): Observable<WorkerDto> {
    return this.http.get<WorkerDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Obtener estadísticas de workers
   */
  getWorkerStats(): Observable<WorkerStatsDto> {
    return this.http.get<WorkerStatsDto>(`${this.apiUrl}/stats`);
  }

  /**
   * Desactivar worker
   */
  deactivateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Activar worker
   */
  activateWorker(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/activate`, {});
  }
}
