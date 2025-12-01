import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WorkerService } from '../../services/worker.service';
import { 
  WorkerDto, 
  WorkerStatsDto,
  WorkerStatus,
  WorkerStatusLabels,
  WorkerStatusColors,
  WorkerStatusIcons
} from '../../models/worker.models';
import { RelativeTimePipe } from '../../../../core/pipes/relative-time.pipe';
import { WorkerDetailComponent } from '../worker-detail/worker-detail.component';

declare var KTMenu: any;

@Component({
  selector: 'app-workers',
  standalone: true,
  imports: [CommonModule, FormsModule, RelativeTimePipe, WorkerDetailComponent],
  templateUrl: './workers.component.html',
  styleUrl: './workers.component.css'
})
export class WorkersComponent implements OnInit, OnDestroy {
  workers: WorkerDto[] = [];
  filteredWorkers: WorkerDto[] = [];
  stats?: WorkerStatsDto;
  isLoading = false;
  searchTerm = '';

  // Detail view state
  showDetail = false;
  selectedWorkerId?: string;

  // Enums y labels para el template
  WorkerStatus = WorkerStatus;
  WorkerStatusLabels = WorkerStatusLabels;
  WorkerStatusColors = WorkerStatusColors;
  WorkerStatusIcons = WorkerStatusIcons;

  // Mensajes de alerta
  alertMessage: string = '';
  alertType: 'success' | 'danger' | 'info' | 'warning' = 'info';
  showAlert: boolean = false;

  constructor(
    private workerService: WorkerService
  ) {}

  ngOnInit(): void {
    this.loadWorkers();
    this.loadStats();
    // No iniciamos polling automático
  }

  ngOnDestroy(): void {
    // No hay polling que detener
  }

  /**
   * Inicializar menús dropdown de Metronic
   */
  private initMenus(): void {
    setTimeout(() => {
      if (typeof KTMenu !== 'undefined') {
        KTMenu.createInstances();
      }
    }, 100);
  }

  /**
   * Cargar todos los workers
   */
  loadWorkers(): void {
    this.isLoading = true;
    this.workerService.getAllWorkers().subscribe({
      next: (workers) => {
        this.workers = workers;
        this.applyFilter();
        this.isLoading = false;
        this.initMenus();
      },
      error: (error) => {
        console.error('Error cargando workers:', error);
        this.isLoading = false;
        this.showErrorAlert('Error al cargar los workers: ' + (error.error?.message || 'Error desconocido'));
      }
    });
  }

  /**
   * Cargar estadísticas
   */
  loadStats(): void {
    this.workerService.getWorkerStats().subscribe({
      next: (stats) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Error cargando estadísticas:', error);
      }
    });
  }

  /**
   * Refrescar datos manualmente
   */
  refresh(): void {
    this.loadWorkers();
    this.loadStats();
    this.showInfoAlert('Datos actualizados');
  }

  /**
   * Aplicar filtro de búsqueda
   */
  applyFilter(): void {
    const term = this.searchTerm.toLowerCase();
    this.filteredWorkers = this.workers.filter(worker =>
      worker.name.toLowerCase().includes(term) ||
      (worker.hostname && worker.hostname.toLowerCase().includes(term)) ||
      (worker.ipAddress && worker.ipAddress.toLowerCase().includes(term)) ||
      (worker.description && worker.description.toLowerCase().includes(term))
    );
  }

  /**
   * Activar/Desactivar worker
   */
  toggleActive(worker: WorkerDto): void {
    const action = worker.isActive ? 'desactivar' : 'activar';
    
    if (worker.isActive) {
      this.workerService.deactivateWorker(worker.id).subscribe({
        next: () => {
          worker.isActive = false;
          this.showSuccessAlert(`Worker "${worker.name}" desactivado exitosamente`);
        },
        error: (error) => {
          console.error('Error desactivando worker:', error);
          this.showErrorAlert('Error al desactivar worker: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    } else {
      this.workerService.activateWorker(worker.id).subscribe({
        next: () => {
          worker.isActive = true;
          this.showSuccessAlert(`Worker "${worker.name}" activado exitosamente`);
        },
        error: (error) => {
          console.error('Error activando worker:', error);
          this.showErrorAlert('Error al activar worker: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    }
  }

  /**
   * Obtener color del badge de estado
   */
  getStatusBadgeClass(status: WorkerStatus): string {
    return `badge-light-${WorkerStatusColors[status]}`;
  }

  /**
   * Calcular tiempo desde último heartbeat
   */
  getHeartbeatStatus(lastHeartbeat: string): 'recent' | 'warning' | 'critical' {
    const now = new Date();
    const heartbeatDate = new Date(lastHeartbeat);
    const diffSeconds = (now.getTime() - heartbeatDate.getTime()) / 1000;

    if (diffSeconds < 60) return 'recent';
    if (diffSeconds < 300) return 'warning'; // 5 minutos
    return 'critical';
  }

  /**
   * Formatear bytes
   */
  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Mostrar alerta de éxito
   */
  showSuccessAlert(message: string): void {
    this.alertType = 'success';
    this.alertMessage = message;
    this.showAlert = true;
    setTimeout(() => {
      this.showAlert = false;
    }, 5000);
  }

  /**
   * Mostrar alerta de error
   */
  showErrorAlert(message: string): void {
    this.alertType = 'danger';
    this.alertMessage = message;
    this.showAlert = true;
    setTimeout(() => {
      this.showAlert = false;
    }, 5000);
  }

  /**
   * Mostrar alerta de info
   */
  showInfoAlert(message: string): void {
    this.alertType = 'info';
    this.alertMessage = message;
    this.showAlert = true;
    setTimeout(() => {
      this.showAlert = false;
    }, 5000);
  }

  /**
   * Cerrar alerta
   */
  closeAlert(): void {
    this.showAlert = false;
  }

  /**
   * Ver detalles de un worker
   */
  viewWorkerDetail(workerId: string): void {
    this.selectedWorkerId = workerId;
    this.showDetail = true;
  }

  /**
   * Cerrar vista de detalles
   */
  closeDetail(): void {
    this.showDetail = false;
    this.selectedWorkerId = undefined;
  }
}
