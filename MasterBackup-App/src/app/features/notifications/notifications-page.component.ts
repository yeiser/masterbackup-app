import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { NotificationsService, Notification } from '../../core/services/notifications.service';
import { SignalRService } from '../../core/services/signalr.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.css'
})
export class NotificationsPageComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  filteredNotifications: Notification[] = [];
  isLoading = false;
  isLoadingMore = false;
  currentPage = 1;
  pageSize = 20;
  hasMore = true;
  
  // Filtros
  selectedFilter: 'all' | 'unread' | 'read' = 'all';
  selectedType: string = 'all';
  notificationTypes: string[] = [];
  
  private destroy$ = new Subject<void>();

  constructor(
    private notificationsService: NotificationsService,
    private signalRService: SignalRService,
    private router: Router
  ) {}

  ngOnInit(): void {
    console.log('✓ NotificationsPage: Iniciado');
    this.loadNotifications();
    this.subscribeToRealTimeUpdates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Detectar scroll al final de la página para lazy loading
   */
  @HostListener('window:scroll', ['$event'])
  onScroll(): void {
    const windowHeight = window.innerHeight;
    const documentHeight = document.documentElement.scrollHeight;
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    
    // Si estamos cerca del final (100px antes) y no estamos cargando
    if (windowHeight + scrollTop >= documentHeight - 100) {
      if (!this.isLoadingMore && this.hasMore) {
        this.loadMoreNotifications();
      }
    }
  }

  /**
   * Cargar notificaciones iniciales
   */
  loadNotifications(): void {
    this.isLoading = true;
    this.currentPage = 1;
    this.notifications = [];
    
    const isRead = this.selectedFilter === 'all' ? undefined : this.selectedFilter === 'read';
    
    this.notificationsService.getNotifications(this.currentPage, this.pageSize, isRead)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (Array.isArray(response)) {
            this.notifications = response;
            this.hasMore = response.length === this.pageSize;
          }
          this.extractNotificationTypes();
          this.applyFilters();
          this.isLoading = false;
        },
        error: (error) => {
          console.error('✗ Error al cargar notificaciones', error);
          this.isLoading = false;
        }
      });
  }

  /**
   * Cargar más notificaciones (lazy loading)
   */
  loadMoreNotifications(): void {
    if (this.isLoadingMore || !this.hasMore) return;
    
    this.isLoadingMore = true;
    this.currentPage++;
    
    const isRead = this.selectedFilter === 'all' ? undefined : this.selectedFilter === 'read';
    
    this.notificationsService.getNotifications(this.currentPage, this.pageSize, isRead)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          if (Array.isArray(response)) {
            this.notifications = [...this.notifications, ...response];
            this.hasMore = response.length === this.pageSize;
          }
          this.extractNotificationTypes();
          this.applyFilters();
          this.isLoadingMore = false;
        },
        error: (error) => {
          console.error('✗ Error al cargar más notificaciones', error);
          this.isLoadingMore = false;
        }
      });
  }

  /**
   * Suscribirse a actualizaciones en tiempo real
   */
  subscribeToRealTimeUpdates(): void {
    this.signalRService.newNotification$
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        console.log('✓ Nueva notificación recibida en tiempo real', notification);
        // Agregar al inicio de la lista
        this.notifications.unshift(notification as any);
        this.extractNotificationTypes();
        this.applyFilters();
      });
  }

  /**
   * Extraer tipos únicos de notificaciones
   */
  extractNotificationTypes(): void {
    const types = new Set(this.notifications.map(n => n.type));
    this.notificationTypes = Array.from(types).sort();
  }

  /**
   * Aplicar filtros
   */
  applyFilters(): void {
    let filtered = [...this.notifications];
    
    // Filtrar por tipo
    if (this.selectedType !== 'all') {
      filtered = filtered.filter(n => n.type === this.selectedType);
    }
    
    this.filteredNotifications = filtered;
  }

  /**
   * Cambiar filtro de leídas/no leídas
   */
  onFilterChange(filter: 'all' | 'unread' | 'read'): void {
    this.selectedFilter = filter;
    this.loadNotifications();
  }

  /**
   * Cambiar filtro de tipo
   */
  onTypeFilterChange(type: string): void {
    this.selectedType = type;
    this.applyFilters();
  }

  /**
   * Marcar notificación como leída
   */
  onNotificationClick(notification: Notification): void {
    if (!notification.isRead) {
      this.notificationsService.markAsRead(notification.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            notification.isRead = true;
            notification.readAt = new Date();
          },
          error: (error) => {
            console.error('✗ Error al marcar como leída', error);
          }
        });
    }

    // Navegar si tiene URL
    if (notification.redirectUrl) {
      this.router.navigate([notification.redirectUrl]);
    }
  }

  /**
   * Marcar todas como leídas
   */
  markAllAsRead(): void {
    const unreadNotifications = this.notifications.filter(n => !n.isRead);
    
    if (unreadNotifications.length === 0) return;

    // Aquí deberías tener un endpoint para marcar todas, por ahora marcamos una por una
    unreadNotifications.forEach(notification => {
      this.notificationsService.markAsRead(notification.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            notification.isRead = true;
            notification.readAt = new Date();
          }
        });
    });
  }

  /**
   * Obtener ícono según tipo de notificación
   */
  getNotificationIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'success': 'bi bi-check-circle-fill text-success',
      'error': 'bi bi-x-circle-fill text-danger',
      'warning': 'bi bi-exclamation-triangle-fill text-warning',
      'info': 'bi bi-info-circle-fill text-primary',
      'backup-completed': 'bi bi-database-check text-success',
      'backup-failed': 'bi bi-database-x text-danger'
    };
    return icons[type] || 'bi bi-bell-fill text-primary';
  }

  /**
   * Obtener clase de color según tipo
   */
  getNotificationColorClass(type: string): string {
    const colors: { [key: string]: string } = {
      'success': 'border-success',
      'error': 'border-danger',
      'warning': 'border-warning',
      'info': 'border-primary',
      'backup-completed': 'border-success',
      'backup-failed': 'border-danger'
    };
    return colors[type] || 'border-primary';
  }

  /**
   * Formatear fecha relativa
   */
  getRelativeTime(date: Date): string {
    const now = new Date();
    const notificationDate = new Date(date);
    const diffMs = now.getTime() - notificationDate.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Ahora';
    if (diffMins < 60) return `Hace ${diffMins} min`;
    if (diffHours < 24) return `Hace ${diffHours} h`;
    if (diffDays < 7) return `Hace ${diffDays} días`;
    
    return notificationDate.toLocaleDateString('es-ES');
  }

  /**
   * Formatear tipo para mostrar
   */
  formatType(type: string): string {
    return type.split('-').map(word => 
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  }

  /**
   * Obtener cantidad de notificaciones no leídas
   */
  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  /**
   * Verificar si hay notificaciones no leídas
   */
  get hasUnreadNotifications(): boolean {
    return this.unreadCount > 0;
  }
}
