import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UpdateTenantDto {
  name: string;
  identificacion?: string;
  tipoId?: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  paginaWeb?: string;
}

export interface TenantResponse {
  id: string;
  name: string;
  identificacion?: string;
  tipoId?: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  paginaWeb?: string;
  createdAt: string;
  updatedAt?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private apiUrl = `${environment.apiUrl}/tenants`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener información del tenant actual
   */
  getTenant(): Observable<TenantResponse> {
    return this.http.get<TenantResponse>(this.apiUrl);
  }

  /**
   * Actualizar información del tenant
   */
  updateTenant(data: UpdateTenantDto): Observable<TenantResponse> {
    return this.http.put<TenantResponse>(`${this.apiUrl}`, data);
  }

  /**
   * Obtener API key del tenant
   */
  getApiKey(): Observable<{ apiKey: string }> {
    return this.http.get<{ apiKey: string }>(`${this.apiUrl}/apikey`);
  }

  /**
   * Obtener logs de conexiones de API key
   */
  getApiKeyLogs(page: number = 1, pageSize: number = 20): Observable<any> {
    return this.http.get(`${this.apiUrl}/apikey/logs`, {
      params: { page: page.toString(), pageSize: pageSize.toString() }
    });
  }
}
