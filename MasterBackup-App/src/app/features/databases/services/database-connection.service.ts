import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  DatabaseConnectionDto, 
  CreateDatabaseConnectionDto, 
  UpdateDatabaseConnectionDto,
  TestConnectionResultDto 
} from '../../../core/models/database-connection.models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DatabaseConnectionService {
  private apiUrl = `${environment.apiUrl}/DatabaseConnections`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener todas las conexiones de base de datos
   */
  getAllConnections(): Observable<DatabaseConnectionDto[]> {
    return this.http.get<DatabaseConnectionDto[]>(this.apiUrl);
  }

  /**
   * Obtener una conexión por ID
   */
  getConnectionById(id: string): Observable<DatabaseConnectionDto> {
    return this.http.get<DatabaseConnectionDto>(`${this.apiUrl}/${id}`);
  }

  /**
   * Crear nueva conexión
   */
  createConnection(dto: CreateDatabaseConnectionDto): Observable<DatabaseConnectionDto> {
    return this.http.post<DatabaseConnectionDto>(this.apiUrl, dto);
  }

  /**
   * Actualizar conexión existente
   */
  updateConnection(id: string, dto: UpdateDatabaseConnectionDto): Observable<DatabaseConnectionDto> {
    return this.http.put<DatabaseConnectionDto>(`${this.apiUrl}/${id}`, dto);
  }

  /**
   * Eliminar conexión
   */
  deleteConnection(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Solicitar test de conexión (envía job al Worker via RabbitMQ)
   */
  testConnection(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/test`, {});
  }
}
