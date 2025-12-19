import { Component, OnInit, OnDestroy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { NotificationsService, Notification } from '../../../core/services/notifications.service';
import { SignalRService } from '../../../core/services/signalr.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css',
  styles: [`
    .notification-item {
      border-radius: 0.475rem;
      transition: background-color 0.2s ease;
    }
    .notification-item:hover {
      background-color: #f5f8fa !important;
    }
    .cursor-pointer {
      cursor: pointer;
    }
  `]
})
export class NotificationsComponent implements OnInit, OnDestroy {
  isLoading = false;
  private destroy$ = new Subject<void>();
  
  // Getters para acceder a los signals del servicio
  get notifications() {
    return this.notificationsService.recentNotifications;
  }
  
  get unreadCount() {
    return this.notificationsService.unreadCount;
  }

  constructor(
    private notificationsService: NotificationsService,
    private signalRService: SignalRService,
    private router: Router
  ) {}

  ngOnInit(): void {
    console.log('✓ NotificationsComponent: Iniciado');
    
    // Cargar notificaciones iniciales
    this.loadNotifications();

    // Obtener conteo de no leídas
    this.loadUnreadCount();

    // Escuchar nuevas notificaciones desde SignalR
    this.signalRService.newNotification$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        console.log('✓ NotificationsComponent: Nueva notificación recibida', notification);
        
        // Agregar notificación usando el servicio (actualiza el signal automáticamente)
        this.notificationsService.addNotification(notification as any);

        // Mostrar notificación visual (opcional)
        this.showNotificationToast(notification.title, notification.message);
      });

    // Escuchar eventos de backup completado
    this.signalRService.backupCompleted$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        console.log('Backup completado:', event);
        if (event && event.data) {
          const dbName = event.data.databaseName || event.data.scheduleName || 'Base de datos';
          const size = event.data.backupSizeMB ? event.data.backupSizeMB.toFixed(2) : '0';
          this.showSuccessToast(
            'Backup completado',
            `${dbName} - ${size} MB`
          );
        }
      });

    // Escuchar eventos de backup fallido
    this.signalRService.backupFailed$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        console.log('Backup fallido:', event);
        if (event && event.data) {
          const dbName = event.data.databaseName || event.data.scheduleName || 'Base de datos';
          const errorMsg = event.data.errorMessage || 'Error desconocido';
          this.showErrorToast(
            'Backup fallido',
            `${dbName}: ${errorMsg}`
          );
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Carga las notificaciones del usuario
   */
  loadNotifications(): void {
    console.log('✓ NotificationsComponent: Cargando notificaciones...');
    this.isLoading = true;
    this.notificationsService.getNotifications(1, 10)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          console.log('✓ NotificationsComponent: Respuesta recibida', response);
          // El servicio actualiza automáticamente el signal
          this.isLoading = false;
        },
        error: (error) => {
          console.error('✗ NotificationsComponent: Error al cargar notificaciones', error);
          this.isLoading = false;
        }
      });
  }

  /**
   * Carga el conteo de notificaciones no leídas
   */
  loadUnreadCount(): void {
    console.log('✓ NotificationsComponent: Cargando conteo de no leídas...');
    this.notificationsService.getUnreadCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('✓ NotificationsComponent: Respuesta conteo', response);
          // El servicio actualiza automáticamente el signal
        },
        error: (error) => {
          console.error('✗ NotificationsComponent: Error al cargar conteo de no leídas', error);
        }
      });
  }

  /**
   * Marca una notificación como leída y navega a su URL
   */
  onNotificationClick(notification: Notification): void {
    if (!notification.isRead) {
      this.notificationsService.markAsRead(notification.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            console.log('✓ NotificationsComponent: Notificación marcada como leída', notification.id);
            // El servicio actualiza automáticamente el signal
          },
          error: (error) => {
            console.error('✗ NotificationsComponent: Error al marcar como leída', error);
          }
        });
    }

    // Navegar si tiene URL de redirección
    if (notification.redirectUrl) {
      this.router.navigate([notification.redirectUrl]);
    }
  }

  /**
   * Cerrar el dropdown de notificaciones
   */
  closeDropdown(): void {
    // Usar el método nativo de Bootstrap para cerrar el dropdown
    const dropdown = document.querySelector('[data-kt-menu="true"]');
    if (dropdown) {
      dropdown.classList.remove('show');
    }
  }

  /**
   * Obtiene el icono según el tipo de notificación
   */
  getNotificationIcon(type: string): string {
    switch (type) {
      case 'BackupCompleted':
        return 'bi-check-circle-fill text-success';
      case 'BackupFailed':
        return 'bi-x-circle-fill text-danger';
      case 'BackupStarted':
        return 'bi-play-circle-fill text-primary';
      default:
        return 'bi-info-circle-fill text-info';
    }
  }

  /**
   * Formatea la fecha de manera relativa
   */
  getRelativeTime(date: Date): string {
    const now = new Date();
    const notificationDate = new Date(date);
    const diffMs = now.getTime() - notificationDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Ahora';
    if (diffMins < 60) return `${diffMins} min`;
    if (diffHours < 24) return `${diffHours} h`;
    if (diffDays < 7) return `${diffDays} d`;
    return notificationDate.toLocaleDateString();
  }

  /**
   * Muestra una notificación toast (placeholder)
   */
  private showNotificationToast(title: string, message: string): void {
    // Aquí puedes integrar una librería de toast como ngx-toastr o similar
    console.log(`[TOAST] ${title}: ${message}`);
  }

  /**
   * Muestra un toast de éxito
   */
  private showSuccessToast(title: string, message: string): void {
    console.log(`[SUCCESS TOAST] ${title}: ${message}`);
  }

  /**
   * Muestra un toast de error
   */
  private showErrorToast(title: string, message: string): void {
    console.log(`[ERROR TOAST] ${title}: ${message}`);
  }
}
