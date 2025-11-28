import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { StorageService } from '../services/storage.service';

/**
 * Interceptor para agregar automáticamente el token JWT y API Key a todas las peticiones
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  
  constructor(
    private storageService: StorageService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Solo agregar headers si la request va al API
    if (this.shouldAddAuthHeaders(req.url)) {
      const token = this.storageService.getAuthToken();
      const apiKey = this.storageService.getApiKey();
      
      const headers: any = {};
      
      // Agregar JWT Bearer token si existe
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }
      
      // Agregar API Key si existe
      if (apiKey) {
        headers['X-API-Key'] = apiKey;
      }
      
      // Si hay headers para agregar, clonar la request
      if (Object.keys(headers).length > 0) {
        req = req.clone({ setHeaders: headers });
      }
    }
    
    // Manejar la respuesta y errores
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // Si es 401 Unauthorized, redirigir a login
        if (error.status === 401) {
          this.handleUnauthorized();
        }
        
        // Si es 403 Forbidden, mostrar mensaje
        if (error.status === 403) {
          console.error('Acceso denegado: No tienes permisos para realizar esta acción');
        }
        
        return throwError(() => error);
      })
    );
  }

  /**
   * Determina si debe agregar headers de autenticación
   */
  private shouldAddAuthHeaders(url: string): boolean {
    // Agregar headers solo si es una llamada al API
    // Excluir endpoints públicos si es necesario
    return url.includes('/api/');
  }

  /**
   * Maneja el caso cuando el token es inválido o expiró
   */
  private handleUnauthorized(): void {
    // Limpiar sesión
    this.storageService.clearSession();
    
    // Redirigir a login
    this.router.navigate(['/login']);
  }
}
