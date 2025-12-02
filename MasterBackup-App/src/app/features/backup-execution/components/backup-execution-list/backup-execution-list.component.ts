import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { BackupHistoryService } from '../../services/backup-history.service';
import { 
  BackupHistoryDto, 
  BackupStatus, 
  BackupStatisticsDto,
  BackupHistoryFilters 
} from '../../models/backup-history.models';
import Swal from 'sweetalert2';
import { Subject, takeUntil } from 'rxjs';
import { DatabaseConnectionService } from '../../../databases/services/database-connection.service';
import { DatabaseConnectionDto } from '../../../databases/models/database-connection.models';
import { BackupScheduleService } from '../../../backup-schedule/services/backup-schedule.service';
import { BackupScheduleDto } from '../../../backup-schedule/models/backup-schedule.models';

declare var KTMenu: any;

@Component({
  selector: 'app-backup-execution-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './backup-execution-list.component.html',
  styleUrl: './backup-execution-list.component.css'
})
export class BackupExecutionListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  backupHistories: BackupHistoryDto[] = [];
  filteredBackupHistories: BackupHistoryDto[] = [];
  databaseConnections: DatabaseConnectionDto[] = [];
  backupSchedules: BackupScheduleDto[] = [];
  statistics: BackupStatisticsDto | null = null;
  
  filterForm: FormGroup;
  selectedBackups: Set<string> = new Set();
  selectAll: boolean = false;
  
  // Paginación
  currentPage: number = 1;
  pageSize: number = 20;
  totalCount: number = 0;
  totalPages: number = 0;
  
  // Estados
  BackupStatus = BackupStatus;
  loading: boolean = false;
  showFilters: boolean = true;
  viewMode: 'list' | 'grouped' | 'statistics' = 'list';
  
  // Alertas
  alertVisible: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' | 'info' = 'success';

  // Math para template
  Math = Math;

  constructor(
    private fb: FormBuilder,
    private backupHistoryService: BackupHistoryService,
    private databaseConnectionService: DatabaseConnectionService,
    private backupScheduleService: BackupScheduleService
  ) {
    this.filterForm = this.fb.group({
      searchTerm: [''],
      databaseConnectionId: [''],
      backupScheduleId: [''],
      status: [''],
      isInstantBackup: [''],
      startDateFrom: [''],
      startDateTo: [''],
      sortBy: ['StartTime'],
      sortDirection: ['DESC']
    });
  }

  ngOnInit(): void {
    this.loadDatabaseConnections();
    this.loadBackupSchedules();
    this.loadBackupHistories();
    this.loadStatistics();
    
    // Suscribirse a cambios en filtros
    this.filterForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadBackupHistories();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Cargar conexiones de base de datos
   */
  loadDatabaseConnections(): void {
    this.databaseConnectionService.getAllConnections(true)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (connections) => {
          this.databaseConnections = connections;
        },
        error: (error) => {
          console.error('Error loading database connections:', error);
        }
      });
  }

  /**
   * Cargar programaciones de backup
   */
  loadBackupSchedules(): void {
    this.backupScheduleService.getAllSchedules(undefined, undefined, undefined, 1, 100, true)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (schedules) => {
          this.backupSchedules = schedules;
        },
        error: (error) => {
          console.error('Error loading backup schedules:', error);
        }
      });
  }

  /**
   * Cargar historial de backups
   */
  loadBackupHistories(): void {
    const filters: BackupHistoryFilters = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      ...this.getFiltersFromForm()
    };

    this.backupHistoryService.getBackupHistory(filters)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.backupHistories = result.items;
          this.filteredBackupHistories = result.items;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.currentPage = result.pageNumber;
          
          // Reinicializar menús después de cargar datos
          setTimeout(() => this.initMenus(), 200);
        },
        error: (error) => {
          console.error('Error loading backup histories:', error);
          this.showAlert('Error al cargar el historial de backups', 'error');
        }
      });
  }

  /**
   * Cargar estadísticas
   */
  loadStatistics(): void {
    const filters = this.getFiltersFromForm();
    
    this.backupHistoryService.getBackupStatistics(
      filters.backupScheduleId,
      filters.databaseConnectionId,
      filters.startDateFrom,
      filters.startDateTo,
      true
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (stats) => {
        this.statistics = stats;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
      }
    });
  }

  /**
   * Obtener filtros del formulario
   */
  private getFiltersFromForm(): any {
    const formValue = this.filterForm.value;
    const filters: any = {};

    if (formValue.databaseConnectionId) filters.databaseConnectionId = formValue.databaseConnectionId;
    if (formValue.backupScheduleId) filters.backupScheduleId = formValue.backupScheduleId;
    if (formValue.status) filters.status = formValue.status;
    if (formValue.isInstantBackup !== '') filters.isInstantBackup = formValue.isInstantBackup === 'true';
    if (formValue.startDateFrom) filters.startDateFrom = new Date(formValue.startDateFrom);
    if (formValue.startDateTo) filters.startDateTo = new Date(formValue.startDateTo);
    if (formValue.sortBy) filters.sortBy = formValue.sortBy;
    if (formValue.sortDirection) filters.sortDirection = formValue.sortDirection;

    return filters;
  }

  /**
   * Ver detalles de un backup
   */
  viewBackupDetails(backup: BackupHistoryDto): void {
    const errorHtml = backup.errorMessage 
      ? `<div class="alert alert-danger mt-3">
           <strong>Error:</strong> ${backup.errorMessage}
           ${backup.errorCode ? `<br><strong>Código:</strong> ${backup.errorCode}` : ''}
         </div>`
      : '';

    Swal.fire({
      title: 'Detalles del Backup',
      html: `
        <div class="text-start">
          <div class="mb-3">
            <strong>Base de Datos:</strong> ${backup.databaseConnectionName}<br>
            <strong>Estado:</strong> <span class="badge ${this.getStatusBadgeClass(backup.status)}">${backup.statusText}</span><br>
            ${backup.backupScheduleName ? `<strong>Programación:</strong> ${backup.backupScheduleName}<br>` : ''}
            <strong>Tipo:</strong> ${backup.isInstantBackup ? 'Manual' : 'Programado'}<br>
          </div>
          <div class="mb-3">
            <strong>Inicio:</strong> ${this.formatDateTime(backup.startTime)}<br>
            ${backup.endTime ? `<strong>Fin:</strong> ${this.formatDateTime(backup.endTime)}<br>` : ''}
            ${backup.duration ? `<strong>Duración:</strong> ${backup.duration}<br>` : ''}
          </div>
          ${backup.backupSizeMB ? `
          <div class="mb-3">
            <strong>Tamaño:</strong> ${this.formatFileSize(backup.backupSizeMB)}<br>
            ${backup.compressionType ? `<strong>Compresión:</strong> ${backup.compressionType}<br>` : ''}
            ${backup.blobName ? `<strong>Archivo:</strong> ${backup.blobName}<br>` : ''}
          </div>
          ` : ''}
          ${backup.retryCount > 0 ? `
          <div class="mb-3">
            <strong>Reintentos:</strong> ${backup.retryCount}
          </div>
          ` : ''}
          ${errorHtml}
        </div>
      `,
      width: 600,
      confirmButtonText: 'Cerrar',
      confirmButtonColor: '#009ef7'
    });
  }

  /**
   * Reintentar backup
   */
  retryBackup(backup: BackupHistoryDto): void {
    Swal.fire({
      title: '¿Reintentar Backup?',
      text: `Se creará un nuevo backup para ${backup.databaseConnectionName}`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Sí, reintentar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#009ef7',
      cancelButtonColor: '#f1416c'
    }).then((result) => {
      if (result.isConfirmed) {
        this.backupHistoryService.retryBackup(backup.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (response) => {
              this.showAlert('Backup reintentado exitosamente', 'success');
              this.loadBackupHistories();
              this.loadStatistics();
              this.scrollToTop();
            },
            error: (error) => {
              console.error('Error retrying backup:', error);
              this.showAlert('Error al reintentar el backup', 'error');
            }
          });
      }
    });
  }

  /**
   * Descargar backup
   */
  downloadBackup(backup: BackupHistoryDto): void {
    if (!backup.blobUrl) {
      this.showAlert('No hay archivo disponible para descargar', 'error');
      return;
    }

    this.backupHistoryService.getDownloadUrl(backup.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          // Abrir URL en nueva pestaña
          window.open(response.blobUrl, '_blank');
          this.showAlert('Descarga iniciada', 'success');
        },
        error: (error) => {
          console.error('Error getting download URL:', error);
          this.showAlert('Error al obtener la URL de descarga', 'error');
        }
      });
  }

  /**
   * Eliminar backup
   */
  deleteBackup(backup: BackupHistoryDto): void {
    Swal.fire({
      title: '¿Eliminar Registro?',
      text: `Se eliminará el registro del backup de ${backup.databaseConnectionName}. Esta acción no se puede deshacer.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#f1416c',
      cancelButtonColor: '#7e8299'
    }).then((result) => {
      if (result.isConfirmed) {
        this.backupHistoryService.deleteBackupHistory(backup.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.showAlert('Registro eliminado exitosamente', 'success');
              this.loadBackupHistories();
              this.loadStatistics();
              this.scrollToTop();
            },
            error: (error) => {
              console.error('Error deleting backup:', error);
              this.showAlert('Error al eliminar el registro', 'error');
            }
          });
      }
    });
  }

  /**
   * Eliminar backups seleccionados
   */
  deleteSelectedBackups(): void {
    if (this.selectedBackups.size === 0) {
      this.showAlert('No hay backups seleccionados', 'info');
      return;
    }

    Swal.fire({
      title: '¿Eliminar Registros Seleccionados?',
      text: `Se eliminarán ${this.selectedBackups.size} registros. Esta acción no se puede deshacer.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#f1416c',
      cancelButtonColor: '#7e8299'
    }).then((result) => {
      if (result.isConfirmed) {
        const ids = Array.from(this.selectedBackups);
        this.backupHistoryService.bulkDeleteBackupHistory(ids)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (response) => {
              this.showAlert(`${response.deletedCount} de ${response.totalRequested} registros eliminados`, 'success');
              this.selectedBackups.clear();
              this.selectAll = false;
              this.loadBackupHistories();
              this.loadStatistics();
              this.scrollToTop();
            },
            error: (error) => {
              console.error('Error bulk deleting backups:', error);
              this.showAlert('Error al eliminar los registros', 'error');
            }
          });
      }
    });
  }

  /**
   * Seleccionar/deseleccionar todos
   */
  toggleSelectAll(): void {
    this.selectAll = !this.selectAll;
    
    if (this.selectAll) {
      this.filteredBackupHistories.forEach(backup => this.selectedBackups.add(backup.id));
    } else {
      this.selectedBackups.clear();
    }
  }

  /**
   * Toggle selección individual
   */
  toggleSelection(backupId: string): void {
    if (this.selectedBackups.has(backupId)) {
      this.selectedBackups.delete(backupId);
    } else {
      this.selectedBackups.add(backupId);
    }
    
    this.selectAll = this.selectedBackups.size === this.filteredBackupHistories.length;
  }

  /**
   * Verificar si está seleccionado
   */
  isSelected(backupId: string): boolean {
    return this.selectedBackups.has(backupId);
  }

  /**
   * Cambiar vista
   */
  changeView(mode: 'list' | 'grouped' | 'statistics'): void {
    this.viewMode = mode;
    if (mode === 'statistics') {
      this.loadStatistics();
    }
  }

  /**
   * Limpiar filtros
   */
  clearFilters(): void {
    this.filterForm.reset({
      searchTerm: '',
      databaseConnectionId: '',
      backupScheduleId: '',
      status: '',
      isInstantBackup: '',
      startDateFrom: '',
      startDateTo: '',
      sortBy: 'StartTime',
      sortDirection: 'DESC'
    });
    this.currentPage = 1;
    this.loadBackupHistories();
  }

  /**
   * Paginación
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadBackupHistories();
      this.scrollToTop();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    
    let startPage = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
    let endPage = Math.min(this.totalPages, startPage + maxVisible - 1);
    
    if (endPage - startPage < maxVisible - 1) {
      startPage = Math.max(1, endPage - maxVisible + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  /**
   * Helpers de formato
   */
  formatDateTime(date: Date | string | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleString('es-ES', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatFileSize(sizeMB: number | undefined): string {
    if (!sizeMB) return 'N/A';
    if (sizeMB < 1) return `${(sizeMB * 1024).toFixed(2)} KB`;
    if (sizeMB < 1024) return `${sizeMB.toFixed(2)} MB`;
    return `${(sizeMB / 1024).toFixed(2)} GB`;
  }

  formatDuration(duration: string | undefined): string {
    if (!duration) return 'N/A';
    return duration;
  }

  getStatusBadgeClass(status: BackupStatus): string {
    switch (status) {
      case BackupStatus.Completed:
        return 'badge-success';
      case BackupStatus.Failed:
        return 'badge-danger';
      case BackupStatus.InProgress:
        return 'badge-primary';
      case BackupStatus.Pending:
        return 'badge-warning';
      case BackupStatus.Cancelled:
        return 'badge-secondary';
      default:
        return 'badge-light';
    }
  }

  getStatusIcon(status: BackupStatus): string {
    switch (status) {
      case BackupStatus.Completed:
        return 'fa-check-circle';
      case BackupStatus.Failed:
        return 'fa-times-circle';
      case BackupStatus.InProgress:
        return 'fa-spinner fa-spin';
      case BackupStatus.Pending:
        return 'fa-clock';
      case BackupStatus.Cancelled:
        return 'fa-ban';
      default:
        return 'fa-question-circle';
    }
  }

  /**
   * Mostrar alerta
   */
  showAlert(message: string, type: 'success' | 'error' | 'info'): void {
    this.alertMessage = message;
    this.alertType = type;
    this.alertVisible = true;

    setTimeout(() => {
      this.alertVisible = false;
    }, 5000);
  }

  closeAlert(): void {
    this.alertVisible = false;
  }

  /**
   * Scroll to top
   */
  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /**
   * Inicializar menús KT
   */
  private initMenus(): void {
    setTimeout(() => {
      if (typeof KTMenu !== 'undefined') {
        // Destruir instancias existentes
        const menus = document.querySelectorAll('[data-kt-menu="true"]');
        menus.forEach((menu: any) => {
          const menuInstance = KTMenu.getInstance(menu);
          if (menuInstance) {
            menuInstance.destroy();
          }
        });

        // Crear nuevas instancias
        KTMenu.createInstances('[data-kt-menu="true"]');
      }
    }, 200);
  }

  /**
   * Refrescar datos
   */
  refresh(): void {
    this.loadBackupHistories();
    this.loadStatistics();
    this.showAlert('Datos actualizados', 'success');
  }
}
