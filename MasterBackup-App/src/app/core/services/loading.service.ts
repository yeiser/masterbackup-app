import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

/**
 * Servicio para gestionar el estado global de loading
 * Compatible con el spinner de Metronic
 */
@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();
  
  private requestCount = 0;

  /**
   * Incrementa el contador de requests y activa loading
   */
  show(): void {
    this.requestCount++;
    if (this.requestCount === 1) {
      this.loadingSubject.next(true);
      this.showMetronicSpinner();
    }
  }

  /**
   * Decrementa el contador de requests y desactiva loading si llega a 0
   */
  hide(): void {
    this.requestCount--;
    if (this.requestCount <= 0) {
      this.requestCount = 0;
      this.loadingSubject.next(false);
      this.hideMetronicSpinner();
    }
  }

  /**
   * Fuerza el reset del loading (útil para errores)
   */
  reset(): void {
    this.requestCount = 0;
    this.loadingSubject.next(false);
    this.hideMetronicSpinner();
  }

  /**
   * Muestra el spinner global de Metronic
   */
  private showMetronicSpinner(): void {
    const spinnerElement = document.getElementById('kt_app_page_loading');
    if (spinnerElement) {
      spinnerElement.style.display = 'flex';
    }
  }

  /**
   * Oculta el spinner global de Metronic
   */
  private hideMetronicSpinner(): void {
    const spinnerElement = document.getElementById('kt_app_page_loading');
    if (spinnerElement) {
      spinnerElement.style.display = 'none';
    }
  }

  /**
   * Obtiene el estado actual de loading
   */
  isLoading(): boolean {
    return this.loadingSubject.value;
  }
}
