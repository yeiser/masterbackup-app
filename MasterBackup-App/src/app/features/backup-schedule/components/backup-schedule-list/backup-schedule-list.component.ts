import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { BackupScheduleService } from '../../services/backup-schedule.service';
import { DatabaseConnectionService } from '../../../databases/services/database-connection.service';
import { DatabaseConnectionDto } from '../../../databases/models/database-connection.models';
import { BackupExecutionService } from '../../../backup-execution/services/backup-execution.service';
import Swal from 'sweetalert2';
import { 
  BackupScheduleDto,
  CreateBackupScheduleDto,
  UpdateBackupScheduleDto,
  ScheduleType,
  ScheduleTypeLabels,
  ScheduleTypeIcons,
  DatabaseType,
  DatabaseTypeLabels,
  DatabaseTypeIcons,
  BackupStatus,
  BackupStatusLabels,
  BackupStatusColors
} from '../../models/backup-schedule.models';
import { RelativeTimePipe } from '../../../../core/pipes/relative-time.pipe';

declare var bootstrap: any;
declare var KTMenu: any;

@Component({
  selector: 'app-backup-schedule-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RelativeTimePipe],
  templateUrl: './backup-schedule-list.component.html',
  styleUrl: './backup-schedule-list.component.css'
})
export class BackupScheduleListComponent implements OnInit, OnDestroy {
  schedules: BackupScheduleDto[] = [];
  filteredSchedules: BackupScheduleDto[] = [];
  databaseConnections: DatabaseConnectionDto[] = [];
  scheduleForm!: FormGroup;
  isEditMode = false;
  editingId?: string;
  isLoading = false;
  searchTerm = '';
  filterIsActive?: boolean;
  filterDatabaseConnectionId?: string;
  private menuInitTimeout?: any;

  // Enums y labels para el template
  ScheduleType = ScheduleType;
  ScheduleTypeLabels = ScheduleTypeLabels;
  ScheduleTypeIcons = ScheduleTypeIcons;
  DatabaseType = DatabaseType;
  DatabaseTypeLabels = DatabaseTypeLabels;
  DatabaseTypeIcons = DatabaseTypeIcons;
  BackupStatus = BackupStatus;
  BackupStatusLabels = BackupStatusLabels;
  BackupStatusColors = BackupStatusColors;

  // Configuración de Cron
  cronConfig = {
    frequency: 'daily' as 'daily' | 'weekly' | 'monthly' | 'custom',
    hour: 0,
    minute: 0,
    dayOfMonth: 1,
    customExpression: ''
  };

  hours = Array.from({ length: 24 }, (_, i) => i);
  minutes = [0, 15, 30, 45];
  monthDays = Array.from({ length: 31 }, (_, i) => i + 1);
  weekDays = [
    { label: 'Lun', value: 1, selected: false },
    { label: 'Mar', value: 2, selected: false },
    { label: 'Mié', value: 3, selected: false },
    { label: 'Jue', value: 4, selected: false },
    { label: 'Vie', value: 5, selected: false },
    { label: 'Sáb', value: 6, selected: false },
    { label: 'Dom', value: 0, selected: false }
  ];

  // Modales
  private formModal: any;

  // Mensajes de alerta
  alertMessage: string = '';
  alertType: 'success' | 'danger' | 'info' | 'warning' = 'info';
  showAlert: boolean = false;

  constructor(
    private fb: FormBuilder,
    private scheduleService: BackupScheduleService,
    private databaseService: DatabaseConnectionService,
    private backupExecutionService: BackupExecutionService
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadDatabaseConnections();
    this.loadSchedules();
  }

  ngOnDestroy(): void {
    // Limpiar timeout de menús si existe
    if (this.menuInitTimeout) {
      clearTimeout(this.menuInitTimeout);
    }
  }

  private initMenus(): void {
    // Cancelar timeout anterior si existe
    if (this.menuInitTimeout) {
      clearTimeout(this.menuInitTimeout);
    }
    
    this.menuInitTimeout = setTimeout(() => {
      if (typeof KTMenu !== 'undefined') {
        // Destruir todas las instancias antiguas
        const menuElements = document.querySelectorAll('[data-kt-menu="true"]');
        menuElements.forEach((element: any) => {
          const menuInstance = KTMenu.getInstance(element);
          if (menuInstance) {
            menuInstance.destroy();
          }
        });
        
        // Esperar un poco más antes de crear nuevas instancias
        setTimeout(() => {
          KTMenu.createInstances();
        }, 50);
      }
    }, 300);
  }

  initForm(): void {
    this.scheduleForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      databaseConnectionId: ['', Validators.required],
      scheduleType: [ScheduleType.Cron, Validators.required],
      cronExpression: ['', [Validators.required, this.cronValidator()]],
      intervalMinutes: [60, [Validators.min(1)]],
      retentionDays: [30, [Validators.required, Validators.min(1), Validators.max(365)]],
      isActive: [true],
      timeZone: ['UTC'],
      maxRetries: [3, [Validators.required, Validators.min(0), Validators.max(10)]],
      timeoutMinutes: [30, [Validators.required, Validators.min(1), Validators.max(1440)]],
      priority: [5, [Validators.required, Validators.min(1), Validators.max(10)]],
      notificationMode: ['all'] // 'all' | 'failure' | 'none'
    });

    // Inicializar con cron por defecto
    this.setCronFrequency('daily');
  }

  // Validador personalizado para expresiones cron (formato Quartz.NET con 6 campos)
  cronValidator() {
    return (control: any) => {
      const value = control.value;
      if (!value) {
        return null; // Si está vacío, el required lo maneja
      }

      // Validar formato básico: debe tener 6 partes separadas por espacios (Quartz.NET)
      const parts = value.trim().split(/\s+/);
      if (parts.length !== 6) {
        return { invalidCron: 'La expresión debe tener exactamente 6 partes (Quartz.NET format): segundo minuto hora día mes díaSemana. Ejemplo: "0 0 2 * * ?"' };
      }

      // Validar cada parte individualmente
      const [second, minute, hour, day, month, weekday] = parts;

      // Función auxiliar para validar campos numéricos con rangos, listas y divisores
      const isValidCronField = (value: string, min: number, max: number, allowQuestionMark = false): boolean => {
        // Permitir * y ? (Quartz.NET usa ? para "sin valor específico")
        if (value === '*' || (allowQuestionMark && value === '?')) return true;
        
        // Permitir */n (cada n unidades)
        if (value.startsWith('*/')) {
          const divisor = parseInt(value.substring(2));
          return !isNaN(divisor) && divisor > 0 && divisor <= max;
        }

        // Permitir n/m (desde n cada m)
        if (value.includes('/') && !value.startsWith('*/')) {
          const [start, divisor] = value.split('/').map(v => parseInt(v));
          return !isNaN(start) && !isNaN(divisor) && start >= min && start <= max && divisor > 0;
        }

        // Permitir listas: 1,2,3
        if (value.includes(',')) {
          return value.split(',').every(v => {
            const num = parseInt(v.trim());
            return !isNaN(num) && num >= min && num <= max;
          });
        }

        // Permitir rangos: 1-5
        if (value.includes('-')) {
          const [start, end] = value.split('-').map(v => parseInt(v.trim()));
          return !isNaN(start) && !isNaN(end) && start >= min && end <= max && start <= end;
        }

        // Permitir un solo número
        const num = parseInt(value);
        return !isNaN(num) && num >= min && num <= max;
      };

      // Validar cada campo con sus rangos específicos (Quartz.NET)
      if (!isValidCronField(second, 0, 59)) {
        return { invalidCron: `Segundo inválido: "${second}". Debe estar entre 0-59 o usar *, */n, rangos, o listas.` };
      }
      if (!isValidCronField(minute, 0, 59)) {
        return { invalidCron: `Minuto inválido: "${minute}". Debe estar entre 0-59 o usar *, */n, rangos, o listas.` };
      }
      if (!isValidCronField(hour, 0, 23)) {
        return { invalidCron: `Hora inválida: "${hour}". Debe estar entre 0-23 o usar *, */n, rangos, o listas.` };
      }
      if (!isValidCronField(day, 1, 31, true)) {
        return { invalidCron: `Día inválido: "${day}". Debe estar entre 1-31, usar *, ?, */n, rangos, o listas.` };
      }
      if (!isValidCronField(month, 1, 12)) {
        return { invalidCron: `Mes inválido: "${month}". Debe estar entre 1-12 o usar *, */n, rangos, o listas.` };
      }
      if (!isValidCronField(weekday, 0, 7, true)) {
        return { invalidCron: `Día de semana inválido: "${weekday}". Debe estar entre 0-7 (0 y 7 = domingo), usar *, ?, */n, rangos, o listas.` };
      }

      return null; // Válido
    };
  }

  loadDatabaseConnections(): void {
    this.databaseService.getAllConnections().subscribe({
      next: (connections) => {
        this.databaseConnections = connections.filter(c => c.isActive);
      },
      error: (error) => {
        console.error('Error cargando conexiones:', error);
        this.showErrorAlert('Error al cargar las conexiones de base de datos');
      }
    });
  }

  loadSchedules(): void {
    this.isLoading = true;
    this.scheduleService.getAllSchedules(
      this.filterDatabaseConnectionId,
      this.filterIsActive,
      this.searchTerm
    ).subscribe({
      next: (schedules) => {
        this.schedules = schedules;
        this.applyFilter();
        this.isLoading = false;
        this.initMenus();
      },
      error: (error) => {
        console.error('Error cargando schedules:', error);
        this.isLoading = false;
        this.showErrorAlert('Error al cargar las programaciones de backup');
      }
    });
  }

  applyFilter(): void {
    const term = this.searchTerm.toLowerCase();
    this.filteredSchedules = this.schedules.filter(schedule =>
      schedule.name.toLowerCase().includes(term) ||
      schedule.databaseConnectionName.toLowerCase().includes(term) ||
      (schedule.description && schedule.description.toLowerCase().includes(term))
    );
  }

  onSearchChange(): void {
    this.applyFilter();
  }

  onFilterActiveChange(value: string): void {
    this.filterIsActive = value === '' ? undefined : value === 'true';
    this.loadSchedules();
  }

  onFilterDatabaseChange(value: string): void {
    this.filterDatabaseConnectionId = value === '' ? undefined : value;
    this.loadSchedules();
  }

  refresh(): void {
    this.loadSchedules();
  }

  openCreateModal(): void {
    this.isEditMode = false;
    this.editingId = undefined;
    this.showAlert = false;
    
    // Reset cron config
    this.cronConfig = {
      frequency: 'daily',
      hour: 0,
      minute: 0,
      dayOfMonth: 1,
      customExpression: ''
    };
    this.weekDays.forEach(day => day.selected = false);
    
    this.scheduleForm.reset({
      scheduleType: ScheduleType.Cron,
      retentionDays: 30,
      isActive: true,
      timeZone: 'UTC',
      maxRetries: 3,
      timeoutMinutes: 30,
      priority: 5,
      notificationMode: 'all'
    });
    this.setCronFrequency('daily');
    this.showFormModal();
  }

  openEditModal(schedule: BackupScheduleDto): void {
    this.isEditMode = true;
    this.editingId = schedule.id;
    this.showAlert = false;
    
    // Solo soportamos tipo Cron
    const scheduleType = ScheduleType.Cron;
    
    this.scheduleForm.patchValue({
      name: schedule.name,
      description: schedule.description,
      databaseConnectionId: schedule.databaseConnectionId,
      scheduleType: scheduleType,
      cronExpression: schedule.cronExpression,
      intervalMinutes: schedule.intervalMinutes,
      retentionDays: schedule.retentionDays,
      isActive: schedule.isActive,
      timeZone: schedule.timeZone || 'UTC',
      maxRetries: schedule.maxRetries ?? 3,
      timeoutMinutes: schedule.timeoutMinutes ?? 30,
      priority: schedule.priority ?? 5,
      notificationMode: this.getNotificationMode(schedule)
    });

    // Parse cron expression si es tipo Cron
    if (scheduleType === ScheduleType.Cron && schedule.cronExpression) {
      this.parseCronToConfig(schedule.cronExpression);
    }
    
    this.showFormModal();
  }

  saveSchedule(): void {
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }

    const formValue = this.scheduleForm.value;
    this.isLoading = true;

    if (this.isEditMode && this.editingId) {
      const scheduleType = Number(formValue.scheduleType);
      const updateDto: UpdateBackupScheduleDto = {
        name: formValue.name,
        description: formValue.description,
        scheduleType: scheduleType,
        retentionDays: formValue.retentionDays,
        isActive: formValue.isActive,
        timeZone: formValue.timeZone,
        maxRetries: formValue.maxRetries,
        timeoutMinutes: formValue.timeoutMinutes,
        priority: formValue.priority,
        notifyOnCompletion: formValue.notificationMode === 'all' || formValue.notificationMode === 'failure',
        notifyOnlyOnFailure: formValue.notificationMode === 'failure'
      };

      // Solo soportamos tipo Cron
      updateDto.cronExpression = formValue.cronExpression;

      console.log('Updating schedule with DTO:', updateDto); // Debug

      this.scheduleService.updateSchedule(this.editingId, updateDto).subscribe({
        next: () => {
          this.isLoading = false;
          this.hideFormModal();
          this.loadSchedules();
          // Mostrar alert después de cerrar modal y hacer scroll al top
          setTimeout(() => {
            this.showSuccessAlert('Programación de backup actualizada exitosamente');
            this.scrollToTop();
          }, 300);
        },
        error: (error) => {
          console.error('Error actualizando programación de backup:', error);
          this.isLoading = false;
          this.showErrorAlert('Error al actualizar la programación de backup: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    } else {
      const scheduleType = Number(formValue.scheduleType);
      const createDto: CreateBackupScheduleDto = {
        name: formValue.name,
        description: formValue.description,
        databaseConnectionId: formValue.databaseConnectionId,
        scheduleType: scheduleType,
        retentionDays: formValue.retentionDays,
        isActive: formValue.isActive,
        timeZone: formValue.timeZone,
        maxRetries: formValue.maxRetries,
        timeoutMinutes: formValue.timeoutMinutes,
        priority: formValue.priority,
        notifyOnCompletion: formValue.notificationMode === 'all' || formValue.notificationMode === 'failure',
        notifyOnlyOnFailure: formValue.notificationMode === 'failure'
      };

      // Solo soportamos tipo Cron
      createDto.cronExpression = formValue.cronExpression;

      console.log('Creating schedule with DTO:', createDto); // Debug

      this.scheduleService.createSchedule(createDto).subscribe({
        next: () => {
          this.isLoading = false;
          this.hideFormModal();
          this.loadSchedules();
          // Mostrar alert después de cerrar modal y hacer scroll al top
          setTimeout(() => {
            this.showSuccessAlert('Programación de backup creada exitosamente');
            this.scrollToTop();
          }, 300);
        },
        error: (error) => {
          console.error('Error creando programación de backup:', error);
          this.isLoading = false;
          this.showErrorAlert('Error al crear la programación de backup: ' + (error.error?.message || 'Error desconocido'));
        }
      });
    }
  }

  pauseSchedule(schedule: BackupScheduleDto): void {
    this.scheduleService.pauseSchedule(schedule.id).subscribe({
      next: () => {
        this.showSuccessAlert(`Programación de backup "${schedule.name}" pausada`);
        this.loadSchedules();
      },
      error: (error) => {
        console.error('Error pausando programación de backup:', error);
        this.showErrorAlert('Error al pausar la programación de backup');
      }
    });
  }

  resumeSchedule(schedule: BackupScheduleDto): void {
    this.scheduleService.resumeSchedule(schedule.id).subscribe({
      next: () => {
        this.showSuccessAlert(`Programación de backup "${schedule.name}" reanudada`);
        this.loadSchedules();
      },
      error: (error) => {
        console.error('Error reanudando programación de backup:', error);
        this.showErrorAlert('Error al reanudar la programación de backup');
      }
    });
  }

  executeNow(schedule: BackupScheduleDto): void {
    Swal.fire({
      title: '¿Ejecutar backup ahora?',
      html: `
        <div class="text-start">
          <p>¿Deseas ejecutar un backup instantáneo de:</p>
          <div class="alert alert-light-primary d-flex align-items-center mt-3 mb-3">
            <i class="fa fa-database fs-2x text-primary me-3"></i>
            <div>
              <div class="fw-bold fs-5">${schedule.name}</div>
              <div class="text-muted fs-7">${schedule.databaseConnectionName}</div>
            </div>
          </div>
          <p class="text-muted fs-7">
            <i class="fa fa-info-circle me-1"></i>
            El backup se ejecutará inmediatamente y podrás ver su progreso en el historial de backups.
          </p>
        </div>
      `,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: '<i class="fa fa-play me-2"></i>Sí, ejecutar ahora',
      cancelButtonText: '<i class="fa fa-times me-2"></i>Cancelar',
      buttonsStyling: false,
      customClass: {
        confirmButton: 'btn btn-primary',
        cancelButton: 'btn btn-light'
      },
      showLoaderOnConfirm: true,
      preConfirm: () => {
        return this.backupExecutionService.executeInstantBackup({
          databaseConnectionId: schedule.databaseConnectionId,
          compressionType: 'GZIP',
          timeoutMinutes: schedule.timeoutMinutes,
          maxRetries: schedule.maxRetries
        }).toPromise()
        .then(response => {
          return response;
        })
        .catch(error => {
          Swal.showValidationMessage(
            `Error: ${error.error?.message || error.message || 'No se pudo ejecutar el backup'}`
          );
        });
      },
      allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        Swal.fire({
          icon: 'success',
          title: '¡Backup en cola!',
          html: `
            <div class="text-start">
              <p class="mb-3">El backup se ha encolado exitosamente:</p>
              <div class="alert alert-light-success">
                <div class="d-flex align-items-center mb-2">
                  <i class="fa fa-check-circle text-success me-2"></i>
                  <span class="fw-bold">Estado:</span>
                  <span class="ms-2">En cola</span>
                </div>
                <div class="d-flex align-items-center mb-2">
                  <i class="fa fa-fingerprint text-primary me-2"></i>
                  <span class="fw-bold">Job ID:</span>
                  <code class="ms-2">${result.value.jobId.substring(0, 8)}...</code>
                </div>
                <div class="d-flex align-items-center">
                  <i class="fa fa-clock text-info me-2"></i>
                  <span class="fw-bold">Hora:</span>
                  <span class="ms-2">${new Date(result.value.queuedAt).toLocaleString('es-ES')}</span>
                </div>
              </div>
              <p class="text-muted fs-7 mb-0">
                <i class="fa fa-info-circle me-1"></i>
                ${result.value.message}
              </p>
            </div>
          `,
          confirmButtonText: '<i class="fa fa-history me-2"></i>Ver historial',
          showCancelButton: true,
          cancelButtonText: 'Cerrar',
          buttonsStyling: false,
          customClass: {
            confirmButton: 'btn btn-primary',
            cancelButton: 'btn btn-light'
          }
        }).then((historyResult) => {
          if (historyResult.isConfirmed) {
            // Navegar al historial de backups
            window.location.href = '/backup-history';
          }
        });
        
        // Refrescar la lista de schedules
        this.loadSchedules();
      }
    });
  }

  confirmDelete(schedule: BackupScheduleDto): void {
    Swal.fire({
      title: '¿Estás seguro?',
      html: `¿Deseas eliminar la programación de backup <strong>${schedule.name}</strong>?${
        schedule.backupHistoryCount > 0
          ? `<br><br><span class="text-warning"><i class="fa fa-exclamation-triangle"></i> Esta programación de backup tiene ${schedule.backupHistoryCount} backup(s) en historial.</span>`
          : ''
      }`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      buttonsStyling: false,
      customClass: {
        confirmButton: 'btn btn-danger',
        cancelButton: 'btn btn-secondary'
      }
    }).then((result) => {
      if (result.isConfirmed) {
        this.deleteSchedule(schedule.id);
      }
    });
  }

  deleteSchedule(id: string): void {
    this.isLoading = true;
    this.scheduleService.deleteSchedule(id).subscribe({
      next: () => {
        this.isLoading = false;
        this.showSuccessAlert('Programación de backup eliminada exitosamente');
        this.loadSchedules();
      },
      error: (error) => {
        console.error('Error eliminando programación de backup:', error);
        this.isLoading = false;
        this.showErrorAlert('Error al eliminar la programación de backup: ' + (error.error?.message || 'Error desconocido'));
      }
    });
  }

  private getNotificationMode(schedule: BackupScheduleDto): string {
    if (!schedule.notifyOnCompletion && !schedule.notifyOnlyOnFailure) {
      return 'none';
    }
    if (schedule.notifyOnlyOnFailure) {
      return 'failure';
    }
    return 'all';
  }

  getScheduleDescription(schedule: BackupScheduleDto): string {
    // Inferir el tipo de schedule si no viene del backend
    const isCronType = schedule.scheduleType === ScheduleType.Cron || 
                       (schedule.cronExpression && !schedule.intervalMinutes);
    
    if (isCronType && schedule.cronExpression) {
      const description = this.getCronDescription(schedule.cronExpression);
      return description;
    } else if (schedule.intervalMinutes) {
      return `Cada ${schedule.intervalMinutes} minutos`;
    }
    
    return 'No configurado';
  }

  getDatabaseTypeLabel(type: DatabaseType | string | number): string {
    const typeNum = typeof type === 'number' ? type : Number(type);
    return DatabaseTypeLabels[typeNum as DatabaseType] || 'Desconocido';
  }

  setCronFrequency(frequency: 'daily' | 'weekly' | 'monthly' | 'custom'): void {
    this.cronConfig.frequency = frequency;
    
    // Resetear valores por defecto según frecuencia
    if (frequency === 'daily') {
      this.cronConfig.hour = 0;
      this.cronConfig.minute = 0;
    } else if (frequency === 'weekly') {
      this.cronConfig.hour = 0;
      this.cronConfig.minute = 0;
      this.weekDays.forEach(day => day.selected = false);
      this.weekDays[0].selected = true; // Lunes por defecto
    } else if (frequency === 'monthly') {
      this.cronConfig.dayOfMonth = 1;
      this.cronConfig.hour = 0;
      this.cronConfig.minute = 0;
    }
    
    this.updateCronExpression();
  }

  updateCronExpression(): void {
    let expression = '';

    // Quartz.NET usa formato de 6 campos: segundo minuto hora día mes díaSemana
    // Siempre iniciamos con 0 segundos
    switch (this.cronConfig.frequency) {
      case 'daily':
        // 0 minuto hora * * ?
        expression = `0 ${this.cronConfig.minute} ${this.cronConfig.hour} * * ?`;
        break;

      case 'weekly':
        // 0 minuto hora ? * diasSemana
        const selectedDays = this.weekDays
          .filter(day => day.selected)
          .map(day => day.value)
          .sort()
          .join(',');
        expression = `0 ${this.cronConfig.minute} ${this.cronConfig.hour} ? * ${selectedDays || '1'}`;
        break;

      case 'monthly':
        // 0 minuto hora dia * ?
        expression = `0 ${this.cronConfig.minute} ${this.cronConfig.hour} ${this.cronConfig.dayOfMonth} * ?`;
        break;

      case 'custom':
        expression = this.cronConfig.customExpression;
        break;
    }

    this.scheduleForm.patchValue({ cronExpression: expression });
  }

  onCustomCronChange(value: string): void {
    this.cronConfig.customExpression = value;
    this.scheduleForm.patchValue({ cronExpression: value });
  }

  parseCronToConfig(cron: string): void {
    if (!cron) return;

    const parts = cron.trim().split(' ');
    
    // Quartz.NET usa 6 campos: segundo minuto hora día mes díaSemana
    if (parts.length !== 6) {
      this.cronConfig.frequency = 'custom';
      this.cronConfig.customExpression = cron;
      return;
    }

    const [second, minute, hour, day, month, weekday] = parts;

    // Detectar tipo (ignoramos segundos, siempre debería ser 0)
    if ((day === '*' || day === '?') && month === '*' && (weekday === '*' || weekday === '?')) {
      // Diario
      this.cronConfig.frequency = 'daily';
      this.cronConfig.minute = parseInt(minute) || 0;
      this.cronConfig.hour = parseInt(hour) || 0;
    } else if ((day === '*' || day === '?') && month === '*' && weekday !== '*' && weekday !== '?') {
      // Semanal
      this.cronConfig.frequency = 'weekly';
      this.cronConfig.minute = parseInt(minute) || 0;
      this.cronConfig.hour = parseInt(hour) || 0;
      
      // Parsear días seleccionados
      this.weekDays.forEach(d => d.selected = false);
      const days = weekday.split(',').map(d => parseInt(d));
      days.forEach(dayNum => {
        const dayObj = this.weekDays.find(d => d.value === dayNum);
        if (dayObj) dayObj.selected = true;
      });
    } else if (day !== '*' && day !== '?' && month === '*' && (weekday === '*' || weekday === '?')) {
      // Mensual
      this.cronConfig.frequency = 'monthly';
      this.cronConfig.minute = parseInt(minute) || 0;
      this.cronConfig.hour = parseInt(hour) || 0;
      this.cronConfig.dayOfMonth = parseInt(day) || 1;
    } else {
      // Personalizado
      this.cronConfig.frequency = 'custom';
      this.cronConfig.customExpression = cron;
    }
  }

  getCronDescription(cron: string | null): string {
    if (!cron) return 'Sin configurar';
    
    const parts = cron.trim().split(' ');
    
    // Soportar tanto formato Unix (5 campos) como Quartz.NET (6 campos)
    let minute: string, hour: string, day: string, month: string, weekday: string;
    
    if (parts.length === 6) {
      // Formato Quartz.NET: segundo minuto hora día mes díaSemana
      [, minute, hour, day, month, weekday] = parts;
    } else if (parts.length === 5) {
      // Formato Unix: minuto hora día mes díaSemana
      [minute, hour, day, month, weekday] = parts;
    } else {
      return cron;
    }

    // Formatear hora
    const formatTime = (h: string, m: string): string => {
      const hourNum = parseInt(h);
      const minNum = parseInt(m);
      return `${hourNum.toString().padStart(2, '0')}:${minNum.toString().padStart(2, '0')}`;
    };

    // Diario (Quartz.NET usa ? para indicar "no importa" en día o díaSemana)
    if ((day === '*' || day === '?') && month === '*' && (weekday === '*' || weekday === '?')) {
      if (minute.startsWith('*/') && hour === '*') {
        return `Cada ${minute.substring(2)} minutos`;
      }
      if (hour.startsWith('*/') && minute === '0') {
        return `Cada ${hour.substring(2)} horas`;
      }
      return `Diariamente a las ${formatTime(hour, minute)}`;
    }

    // Semanal (Quartz.NET usa ? en día cuando se especifica díaSemana)
    if ((day === '*' || day === '?') && month === '*' && weekday !== '*' && weekday !== '?') {
      const dayNames: Record<string, string> = {
        '0': 'domingos', '1': 'lunes', '2': 'martes', 
        '3': 'miércoles', '4': 'jueves', '5': 'viernes', '6': 'sábados', '7': 'domingos'
      };
      
      const days = weekday.split(',').map(d => dayNames[d] || d).join(', ');
      return `${days} a las ${formatTime(hour, minute)}`;
    }

    // Mensual (Quartz.NET usa ? en díaSemana cuando se especifica día)
    if (day !== '*' && day !== '?' && month === '*' && (weekday === '*' || weekday === '?')) {
      return `Día ${day} de cada mes a las ${formatTime(hour, minute)}`;
    }

    return cron;
  }

  private showFormModal(): void {
    const modalElement = document.getElementById('kt_modal_schedule_form');
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

  private showSuccessAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'success';
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
  }

  private showErrorAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'danger';
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 8000);
  }

  private showInfoAlert(message: string): void {
    this.alertMessage = message;
    this.alertType = 'info';
    this.showAlert = true;
    setTimeout(() => this.showAlert = false, 5000);
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
