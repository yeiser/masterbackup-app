import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UpdateProfileDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
}

export interface UserProfileResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: string;
  tenantName?: string;
  phoneNumber?: string;
  createdAt: string;
  isActive: boolean;
  twoFactorEnabled: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  /**
   * Obtener perfil del usuario actual
   */
  getProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrl}/profile`);
  }

  /**
   * Actualizar perfil del usuario
   */
  updateProfile(data: UpdateProfileDto): Observable<UserProfileResponse> {
    return this.http.put<UserProfileResponse>(`${this.apiUrl}/profile`, data);
  }

  /**
   * Validar contraseña actual
   */
  verifyPassword(password: string): Observable<{ valid: boolean }> {
    return this.http.post<{ valid: boolean }>(`${this.apiUrl}/verify-password`, { password });
  }

  /**
   * Cambiar contraseña
   */
  changePassword(data: { currentPassword: string; newPassword: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/change-password`, data);
  }

  /**
   * Alternar autenticación de dos factores
   */
  toggle2FA(enable: boolean): Observable<{ enabled: boolean; message: string }> {
    return this.http.post<{ enabled: boolean; message: string }>(`${this.apiUrl}/toggle-2fa`, { enable });
  }
}

