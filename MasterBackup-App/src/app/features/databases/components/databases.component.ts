import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { DatabaseConnectionService } from '../services/database-connection.service';
import { WorkerService } from '../../workers/services/worker.service';
import { WorkerDto } from '../../workers/models/worker.models';
import Swal from 'sweetalert2';
import { 
  DatabaseConnectionDto, 
  CreateDatabaseConnectionDto,
  UpdateDatabaseConnectionDto,
  DatabaseType,
  DatabaseTypeLabels,
  DatabaseTypeIcons,
  WorkerAssignmentMode,
  WorkerAssignmentModeLabels
} from '../../../core/models/database-connection.models';
import { RelativeTimePipe } from '../../../core/pipes/relative-time.pipe';

declare var bootstrap: any;
declare var KTMenu: any;

@Component({
  selector: 'app-databases',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RelativeTimePipe],
  templateUrl: './databases.component.html',
  styleUrl: './databases.component.css'
})
export class DatabasesComponent implements OnInit, OnDestroy {
  connections: DatabaseConnectionDto[] = [];
  filteredConnections: DatabaseConnectionDto[] = [];
  connectionForm!: FormGroup;
  isEditMode = false;
  editingId?: string;
  isLoading = false;
  searchTerm = '';
  testingConnectionId?: string;
  private pollingInterval?: any;
  private readonly POLLING_INTERVAL_MS = 3000; // 3 segundos

  // Enums y labels para el template
  DatabaseType = DatabaseType;
  DatabaseTypeLabels = DatabaseTypeLabels;
  DatabaseTypeIcons = DatabaseTypeIcons;
  WorkerAssignmentMode = WorkerAssignmentMode;
  WorkerAssignmentModeLabels = WorkerAssignmentModeLabels;
  databaseTypes = Object.keys(DatabaseType).filter(key => !isNaN(Number(key))).map(key => ({
    value: Number(key),
    label: DatabaseTypeLabels[Number(key) as DatabaseType]
  }));
  
  // Workers disponibles
  workers: WorkerDto[] = [];
  workerAssignmentModes = Object.keys(WorkerAssignmentMode).filter(key => !isNaN(Number(key))).map(key => ({
    value: Number(key),
    label: WorkerAssignmentModeLabels[Number(key) as WorkerAssignmentMode]
  }));

  // Modales
  private formModal: any;

  // Mensajes de alerta
  alertMessage: string = '';
  alertType: 'success' | 'danger' | 'info' | 'warning' = 'info';
  showAlert: boolean = false;

  constructor(
    private fb: FormBuilder,
    private databaseService: DatabaseConnectionService,
    private workerService: WorkerService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadConnections();
    this.loadWorkers();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  /**
   * Iniciar polling automático para actualizar conexiones
   */
  private startPolling(): void {
    this.stopPolling();
    this.pollingInterval = setInterval(() => {
      this.loadConnectionsSilently();
    }, this.POLLING_INTERVAL_MS);
  }

  /**
   * Detener polling
   */
  private stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = undefined;
    }
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
   * Inicializar el formulario
   */
  initForm(): void {
    this.connectionForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      type: [DatabaseType.PostgreSQL, Validators.required],
      host: ['', [Validators.required, Validators.maxLength(255)]],
      port: [5432, [Validators.required, Validators.min(1), Validators.max(65535)]],
      database: ['', [Validators.required, Validators.maxLength(100)]],
      username: ['', [Validators.required, Validators.maxLength(100)]],
      password: ['', [Validators.required, Validators.maxLength(255)]],
      sslMode: ['prefer', Validators.maxLength(50)],
      isActive: [true],
      assignmentMode: [WorkerAssignmentMode.Auto],
      assignedWorkerId: [null],
      tags: [[]]  // Array de strings
    });

    // Actualizar puerto predeterminado según el tipo de base de datos
    this.connectionForm.get('type')?.valueChanges.subscribe(type => {
      this.updateDefaultPort(type);
    });

    // Limpiar assignedWorkerId cuando se cambie a modo automático
    this.connectionForm.get('assignmentMode')?.valueChanges.subscribe(mode => {
      if (Number(mode) !== 2) { // Si no es modo Dedicado
        this.connectionForm.patchValue({ assignedWorkerId: null });
      }
    });
  }

  /**
   * Actualizar puerto predeterminado según el tipo de BD
   */
  updateDefaultPort(type: DatabaseType): void {
    const portControl = this.connectionForm.get('port');
    if (!portControl?.dirty) {
      switch (type) {
        case DatabaseType.PostgreSQL:
          portControl?.setValue(5432);
          break;
        case DatabaseType.MySQL:
        case DatabaseType.MariaDB:
          portControl?.setValue(3306);
          break;
        case DatabaseType.SQLServer:
          portControl?.setValue(1433);
          break;
        case DatabaseType.MongoDB:
          portControl?.setValue(27017);
          break;
      }
    }
  }

  /**
   * Refrescar lista de conexiones manualmente
   */
  refresh(): void {
    this.loadConnections();
  }

  /**
   * Cargar workers disponibles
   */
  loadWorkers(): void {
    this.workerService.getAllWorkers().subscribe({
      next: (workers) => {
        this.workers = workers.filter(w => w.isActive);
      },
      error: (error) => {
        console.error('Error cargando workers:', error);
      }
    });
  }

  /**
   * Cargar todas las conexiones
   */
  loadConnections(): void {
    this.isLoading = true;
    this.databaseService.getAllConnections().subscribe({
      next: (connections) => {
        this.connections = connections;
        this.applyFilter();
        this.isLoading = false;
        this.initMenus();
      },
      error: (error) => {
        console.error('Error cargando conexiones:', error);
        this.isLoading = false;
        this.showErrorAlert('Error al cargar las conexiones: ' + (error.error?.message || 'Error desconocido'));
      }
    });
  }

  /**
   * Aplicar filtro de búsqueda
   */
  applyFilter(): void {
    const term = this.searchTerm.toLowerCase();
    this.filteredConnections = this.connections.filter(conn =>
      conn.name.toLowerCase().includes(term) ||
      conn.host.toLowerCase().includes(term) ||
      conn.database.toLowerCase().includes(term) ||
      (conn.description && conn.description.toLowerCase().includes(term))
    );
  }

  /**
   * Abrir modal para crear nueva conexión
   */
  openCreateModal(): void {
    this.isEditMode = false;
    this.editingId = undefined;
    this.showAlert = false; // Limpiar alertas anteriores
    this.connectionForm.reset({
      type: DatabaseType.PostgreSQL,
      port: 5432,
      sslMode: 'prefer',
      isActive: true,
      assignmentMode: WorkerAssignmentMode.Auto,
      assignedWorkerId: null,
      tags: []
    });
    this.connectionForm.get('password')?.setValidators([Validators.required, Validators.maxLength(255)]);
    this.connectionForm.get('password')?.updateValueAndValidity();
    this.showFormModal();
  }

  /**
   * Abrir modal para editar conexión
   */
  openEditModal(connection: DatabaseConnectionDto): void {
    this.isEditMode = true;
    this.editingId = connection.id;
    this.showAlert = false; // Limpiar alertas anteriores
    this.connectionForm.patchValue({
      name: connection.name,
      description: connection.description,
      type: connection.type,
      host: connection.host,
      port: connection.port,
      database: connection.database,
      username: connection.username,
      password: '',
      sslMode: connection.sslMode,
      isActive: connection.isActive,
      assignmentMode: connection.assignmentMode || WorkerAssignmentMode.Auto,
      assignedWorkerId: connection.assignedWorkerId || null,
      tags: connection.tags || []
    });
    // Password es opcional en edición
    this.connectionForm.get('password')?.clearValidators();
    this.connectionForm.get('password')?.updateValueAndValidity();
    this.showFormModal();
  }

  /**
   * Guardar conexión (crear o actualizar)
   */
  saveConnection(): void {
    if (this.connectionForm.invalid) {
      this.connectionForm.markAllAsTouched();
      return;
    }

    const formValue = this.connectionForm.value;

    // Validar que si el modo es Dedicado, se haya seleccionado un worker
    if (Number(formValue.assignmentMode) === 2 && !formValue.assignedWorkerId) {
      this.showErrorAlert('Debes seleccionar un worker cuando el modo de asignación es Dedicado');
      return;
    }

    this.isLoading = true;

    if (this.isEditMode && this.editingId) {
      // Actualizar
      const updateDto: UpdateDatabaseConnectionDto = {
        name: formValue.name,
        description: formValue.description,
        type: formValue.type,
        host: formValue.host,
        port: formValue.port,
        database: formValue.database,
        username: formValue.username,
        password: formValue.password || undefined,
        sslMode: formValue.sslMode,
        isActive: formValue.isActive,
        assignmentMode: Number(formValue.assignmentMode),
        assignedWorkerId: Number(formValue.assignmentMode) === 2 ? formValue.assignedWorkerId || undefined : undefined,
        tags: formValue.tags || []
      };

      this.databaseService.updateConnection(this.editingId, updateDto).subscribe({
        next: () => {
          this.isLoading = false;
          this.showSuccessAlert('Conexión actualizada exitosamente');
          this.hideFormModal();
          this.loadConnections();
        },
        error: (error) => {
          console.error('Error actualizando conexión:', error);
          this.isLoading = false;
          this.showErrorAlert('Error al actualizar la conexión: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    } else {
      // Crear
      const createDto: CreateDatabaseConnectionDto = {
        name: formValue.name,
        description: formValue.description,
        type: formValue.type,
        host: formValue.host,
        port: formValue.port,
        database: formValue.database,
        username: formValue.username,
        password: formValue.password,
        sslMode: formValue.sslMode,
        isActive: formValue.isActive,
        assignmentMode: Number(formValue.assignmentMode),
        assignedWorkerId: Number(formValue.assignmentMode) === 2 ? formValue.assignedWorkerId || undefined : undefined,
        tags: formValue.tags || []
      };

      this.databaseService.createConnection(createDto).subscribe({
        next: () => {
          this.isLoading = false;
          this.showSuccessAlert('Conexión creada exitosamente');
          this.hideFormModal();
          this.loadConnections();
        },
        error: (error) => {
          console.error('Error creando conexión:', error);
          this.isLoading = false;
          this.showErrorAlert('Error al crear la conexión: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    }
  }

  /**
   * Probar conexión por ID (envía job al Worker)
   */
  testConnection(connectionId: string): void {
    const connection = this.connections.find(c => c.id === connectionId);
    if (!connection || this.testingConnectionId) return;

    this.testingConnectionId = connectionId;

    this.databaseService.testConnection(connectionId).subscribe({
      next: () => {
        console.info(`Probando conexión "${connection.name}"... El resultado llegará en breve.`);
        this.showInfoAlert(`⏳ Probando conexión "${connection.name}"... El resultado llegará en breve.`);
        
        // Iniciar polling para actualizar el estado automáticamente
        this.startPolling();
        
        // Detener polling después de 30 segundos (timeout)
        setTimeout(() => {
          if (this.testingConnectionId === connectionId) {
            this.testingConnectionId = undefined;
            this.stopPolling();
          }
        }, 30000);
      },
      error: (error) => {
        console.error('Error enviando solicitud de prueba:', error);
        this.testingConnectionId = undefined;
        this.showErrorAlert('❌ Error al enviar solicitud de prueba: ' + (error.error?.message || 'Error desconocido'));
      }
    });
  }

  /**
   * Probar conexión existente (envía job al Worker)
   */
  testExistingConnection(connection: DatabaseConnectionDto): void {
    this.testConnection(connection.id);
  }

  /**
   * Cargar conexiones silenciosamente (sin mostrar loader)
   */
  private loadConnectionsSilently(): void {
    this.databaseService.getAllConnections().subscribe({
      next: (data) => {
        const previousTestingId = this.testingConnectionId;
        
        // Actualizar datos
        this.connections = data;
        this.applyFilter();
        
        // Verificar si la conexión que estábamos testeando ya tiene resultado
        if (previousTestingId) {
          const testedConnection = this.connections.find(c => c.id === previousTestingId);
          if (testedConnection?.lastTestedAt) {
            const lastTestDate = new Date(testedConnection.lastTestedAt);
            const now = new Date();
            const diffSeconds = (now.getTime() - lastTestDate.getTime()) / 1000;
            
            // Si la última prueba es reciente (menos de 5 segundos), mostrar resultado
            if (diffSeconds < 5) {
              this.testingConnectionId = undefined;
              this.stopPolling();
              
              if (testedConnection.lastTestSuccessful) {
                this.showSuccessAlert(`✓ Conexión "${testedConnection.name}" exitosa: ${testedConnection.lastTestStatus}`);
              } else {
                this.showErrorAlert(`✗ Conexión "${testedConnection.name}" fallida: ${testedConnection.lastTestStatus}`);
              }
            }
          }
        }
      },
      error: (error) => {
        console.error('Error cargando conexiones:', error);
      }
    });
  }

  /**
   * Confirmar eliminación con SweetAlert2
   */
  confirmDelete(connection: DatabaseConnectionDto): void {
    Swal.fire({
      title: '¿Estás seguro?',
      html: `¿Deseas eliminar la conexión <strong>${connection.name}</strong>?${
        connection.backupSchedulesCount > 0
          ? `<br><br><span class="text-warning"><i class="fa fa-exclamation-triangle"></i> Esta conexión tiene ${connection.backupSchedulesCount} programación(es) de respaldo asociada(s).</span>`
          : ''
      }`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      reverseButtons: true
    }).then((result) => {
      if (result.isConfirmed) {
        this.deleteConnection(connection.id);
      }
    });
  }

  /**
   * Eliminar conexión
   */
  private deleteConnection(connectionId: string): void {
    this.isLoading = true;
    this.databaseService.deleteConnection(connectionId).subscribe({
      next: () => {
        this.isLoading = false;
        this.loadConnections();
        Swal.fire({
          title: 'Eliminado',
          text: 'La conexión ha sido eliminada exitosamente',
          icon: 'success',
          timer: 2000,
          showConfirmButton: false
        });
      },
      error: (error) => {
        console.error('Error eliminando conexión:', error);
        this.isLoading = false;
        Swal.fire({
          title: 'Error',
          text: 'Error al eliminar la conexión: ' + (error.error?.message || 'Error desconocido'),
          icon: 'error',
          confirmButtonText: 'Aceptar'
        });
      }
    });
  }

  /**
   * Alternar estado activo/inactivo
   */
  toggleActive(connection: DatabaseConnectionDto): void {
    const updateDto: UpdateDatabaseConnectionDto = {
      name: connection.name,
      description: connection.description,
      type: connection.type,
      host: connection.host,
      port: connection.port,
      database: connection.database,
      username: connection.username,
      sslMode: connection.sslMode,
      isActive: !connection.isActive,
      assignmentMode: Number(connection.assignmentMode),
      assignedWorkerId: connection.assignedWorkerId,
      tags: connection.tags || []
    };
    this.databaseService.updateConnection(connection.id, updateDto).subscribe({
      next: () => {
        this.loadConnections();
        this.showSuccessAlert(`Conexión ${updateDto.isActive ? 'activada' : 'desactivada'} exitosamente`);
      },
      error: (error) => {
        console.error('Error actualizando estado:', error);
        this.showErrorAlert('Error al actualizar el estado de la conexión');
      }
    });
  }

  /**
   * Obtener clase de badge según el tipo de BD
   */
  getBadgeClass(type: DatabaseType): string {
    switch (type) {
      case DatabaseType.PostgreSQL: return 'badge-light-primary';
      case DatabaseType.MySQL: return 'badge-light-info';
      case DatabaseType.SQLServer: return 'badge-light-warning';
      case DatabaseType.MongoDB: return 'badge-light-success';
      case DatabaseType.MariaDB: return 'badge-light-danger';
      default: return 'badge-light-secondary';
    }
  }

  /**
   * Obtener ícono según el tipo de BD
   */
  getIcon(type: DatabaseType): string {
    return DatabaseTypeIcons[type];
  }

  // Métodos de control de modales
  private showFormModal(): void {
    const modalElement = document.getElementById('formModal');
    if (modalElement) {
      this.formModal = new bootstrap.Modal(modalElement);
      this.formModal.show();
    }
  }

  private hideFormModal(): void {
    if (this.formModal) {
      this.formModal.hide();
    }
  }



  // Helpers para validación en el template
  isFieldInvalid(fieldName: string): boolean {
    const field = this.connectionForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getErrorMessage(fieldName: string): string {
    const field = this.connectionForm.get(fieldName);
    if (field?.hasError('required')) return 'Este campo es requerido';
    if (field?.hasError('maxlength')) return `Máximo ${field.errors?.['maxlength'].requiredLength} caracteres`;
    if (field?.hasError('min')) return `Valor mínimo: ${field.errors?.['min'].min}`;
    if (field?.hasError('max')) return `Valor máximo: ${field.errors?.['max'].max}`;
    return '';
  }

  /**
   * Agregar tag
   */
  addTag(event: Event, input: HTMLInputElement): void {
    event.preventDefault();
    const value = input.value.trim();
    if (value) {
      const currentTags = this.connectionForm.get('tags')?.value || [];
      if (!currentTags.includes(value)) {
        this.connectionForm.patchValue({
          tags: [...currentTags, value]
        });
      }
      input.value = '';
    }
  }

  /**
   * Eliminar tag
   */
  removeTag(index: number): void {
    const currentTags = this.connectionForm.get('tags')?.value || [];
    currentTags.splice(index, 1);
    this.connectionForm.patchValue({
      tags: [...currentTags]
    });
  }

  // Métodos para mostrar alertas de Metronic
  private showSuccessAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'success';
    this.showAlert = true;
    this.autoHideAlert();
  }

  private showErrorAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'danger';
    this.showAlert = true;
    this.autoHideAlert();
  }

  private showInfoAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'info';
    this.showAlert = true;
    this.autoHideAlert();
  }

  private showWarningAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'warning';
    this.showAlert = true;
    this.autoHideAlert();
  }

  closeAlert(): void {
    this.showAlert = false;
  }

  private autoHideAlert(): void {
    setTimeout(() => {
      this.showAlert = false;
    }, 5000); // Ocultar después de 5 segundos
  }
}