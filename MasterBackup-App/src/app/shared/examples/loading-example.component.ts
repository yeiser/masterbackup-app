import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingService } from '../../core/services/loading.service';

/**
 * Ejemplo de componente que puede suscribirse al estado global de loading
 * Útil para mostrar spinners locales además del global
 */
@Component({
  selector: 'app-example-with-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card">
      <div class="card-body">
        <!-- Spinner local opcional -->
        <div *ngIf="loadingService.loading$ | async" class="text-center py-5">
          <span class="spinner-border spinner-border-sm me-2"></span>
          Cargando datos...
        </div>

        <!-- Contenido del componente -->
        <div *ngIf="!(loadingService.loading$ | async)">
          <h3>Contenido</h3>
          <p>Este contenido se muestra cuando no hay loading activo</p>
        </div>
      </div>
    </div>
  `
})
export class ExampleWithLoadingComponent {
  constructor(public loadingService: LoadingService) {}
}
