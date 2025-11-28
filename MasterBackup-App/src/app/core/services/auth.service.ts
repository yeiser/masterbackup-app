import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { 
  LoginDto, 
  ValidateEmailDto, 
  Verify2FADto, 
  RegisterDto,
  AuthResponse, 
  EmailValidationResponse,
  SavedAccount 
} from '../models/auth.models';
import { StorageService } from './storage.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private storageService: StorageService
  ) {
    // Cargar usuario actual si existe
    const currentUser = this.storageService.getCurrentUser();
    if (currentUser) {
      this.currentUserSubject.next(currentUser);
    }
  }

  /**
   * Registrar nuevo usuario y tenant
   */
  register(dto: RegisterDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, dto).pipe(
      tap(response => {
        if (response.success && response.token) {
          this.handleSuccessfulLogin(response, dto.email);
        }
      })
    );
  }

  /**
   * Paso 1: Validar si el email existe en el sistema
   */
  validateEmail(email: string): Observable<EmailValidationResponse> {
    const dto: ValidateEmailDto = { email };
    return this.http.post<EmailValidationResponse>(
      `${this.apiUrl}/auth/validate-email`,
      dto
    );
  }

  /**
   * Paso 2: Login con email y contraseña
   */
  login(email: string, password: string): Observable<AuthResponse> {
    const dto: LoginDto = { email, password };
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, dto).pipe(
      tap(response => {
        if (response.success && !response.twoFactorRequired && !response.requiresTwoFactor) {
          this.handleSuccessfulLogin(response, email);
        }
      })
    );
  }

  /**
   * Paso 3: Verificar código 2FA
   */
  verify2FA(email: string, password: string, code: string): Observable<AuthResponse> {
    const dto: Verify2FADto = { 
      email, 
      password, 
      twoFactorCode: code
    };
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/verify-2fa`, dto).pipe(
      tap(response => {
        if (response.success) {
          this.handleSuccessfulLogin(response, email);
        }
      })
    );
  }

  /**
   * Maneja el login exitoso: guarda token, usuario, y cuenta
   */
  private handleSuccessfulLogin(response: AuthResponse, email: string): void {
    // Guardar token
    if (response.token) {
      this.storageService.setAuthToken(response.token);
    }

    // Guardar API Key si está presente (usado en registro)
    if (response.apiKey) {
      this.storageService.setApiKey(response.apiKey);
    }

    // Extraer datos de usuario (pueden venir en response.user o directamente en response)
    const userData = response.user;
    const user = {
      userId: (userData?.id || response.userId) ?? '',
      email: userData?.email || response.email || email,
      firstName: userData?.firstName || response.firstName || '',
      lastName: userData?.lastName || response.lastName || '',
      role: userData?.role || response.role || '',
      tenantId: response.tenantId || ''
    };
    
    this.storageService.setCurrentUser(user);
    this.currentUserSubject.next(user);

    // Guardar cuenta para futuro uso
    const savedAccount: SavedAccount = {
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      lastLogin: new Date()
    };
    this.storageService.saveAccount(savedAccount);
  }

  /**
   * Cerrar sesión (mantiene las cuentas guardadas)
   */
  logout(): void {
    this.storageService.clearSession();
    this.currentUserSubject.next(null);
  }

  /**
   * Obtener cuentas guardadas
   */
  getSavedAccounts(): SavedAccount[] {
    return this.storageService.getSavedAccounts();
  }

  /**
   * Eliminar una cuenta guardada
   */
  removeSavedAccount(email: string): void {
    this.storageService.removeAccount(email);
  }

  /**
   * Verificar si el usuario está autenticado
   */
  isAuthenticated(): boolean {
    return !!this.storageService.getAuthToken();
  }

  /**
   * Obtener token de autenticación
   */
  getAuthToken(): string | null {
    return this.storageService.getAuthToken();
  }

  /**
   * Obtener usuario actual
   */
  getCurrentUser(): any {
    return this.currentUserSubject.value;
  }

  /**
   * Solicitar restablecimiento de contraseña
   */
  forgotPassword(email: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/auth/forgot-password`,
      { email }
    );
  }

  /**
   * Restablecer contraseña con token
   */
  resetPassword(token: string, newPassword: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/auth/reset-password`,
      { token, newPassword }
    );
  }
}
