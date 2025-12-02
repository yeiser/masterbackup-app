import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  DatabaseConnectionDto, 
  CreateDatabaseConnectionDto, 
  UpdateDatabaseConnectionDto,
  TestConnectionResultDto 
} from '../models/database-connection.models';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DatabaseConnectionService {
  private apiUrl = `${environment.apiUrl}/DatabaseConnections`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener todas las conexiones de base de datos
   * @param skipLoading Si es true, no muestra el spinner de loading
   */
  getAllConnections(skipLoading: boolean = false): Observable<DatabaseConnectionDto[]> {
    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.get<DatabaseConnectionDto[]>(this.apiUrl, { headers });
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
   * @param id ID de la conexión a probar
   * @param skipLoading Si es true, no muestra el spinner de loading
   */
  testConnection(id: string, skipLoading: boolean = false): Observable<void> {
    const headers = skipLoading ? new HttpHeaders({ 'X-Skip-Loading': 'true' }) : undefined;
    return this.http.post<void>(`${this.apiUrl}/${id}/test`, {}, { headers });
  }
}
