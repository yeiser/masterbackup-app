import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { finalize, delay } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

/**
 * Interceptor para mostrar/ocultar el spinner de loading durante peticiones HTTP
 * Integrado con el spinner de Metronic
 */
@Injectable()
export class LoadingInterceptor implements HttpInterceptor {
  
  constructor(private loadingService: LoadingService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Verificar si la petición tiene el header X-Skip-Loading
    const skipLoading = req.headers.has('X-Skip-Loading');
    
    // Si tiene el header, removerlo antes de enviar la petición
    if (skipLoading) {
      req = req.clone({
        headers: req.headers.delete('X-Skip-Loading')
      });
    }
    
    // Lista de endpoints que NO deberían mostrar loading
    const excludedUrls = [
      '/api/health',
      '/api/ping',
      // Agregar aquí otros endpoints que no requieran loading visual
    ];

    // Verificar si la URL debe ser excluida
    const shouldShowLoading = !skipLoading && !excludedUrls.some(url => req.url.includes(url));

    if (shouldShowLoading) {
      // Mostrar loading antes de la petición
      this.loadingService.show();
    }

    // Ejecutar la petición y ocultar loading cuando termine
    return next.handle(req).pipe(
      // Agregar un pequeño delay mínimo para evitar flashes (opcional)
      delay(0),
      finalize(() => {
        if (shouldShowLoading) {
          // Ocultar loading después de la petición (éxito o error)
          this.loadingService.hide();
        }
      })
    );
  }
}
