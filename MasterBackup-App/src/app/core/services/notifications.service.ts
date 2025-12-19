import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface Notification {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  redirectUrl?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  isRead: boolean;
  readAt?: Date;
  createdAt: Date;
  expiresAt?: Date;
  metadata?: string;
}

export interface NotificationListResponse {
  success: boolean;
  data: {
    items: Notification[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
  };
  message?: string;
}

export interface UnreadCountResponse {
  success: boolean;
  data: number;
  message?: string;
}

export interface MarkAsReadResponse {
  success: boolean;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationsService {
  private apiUrl = `${environment.apiUrl}/notifications`;
  
  // Signals para estado reactivo
  private notificationsSignal = signal<Notification[]>([]);
  private unreadCountSignal = signal<number>(0);
  
  // Exponer signals como readonly
  public notifications = this.notificationsSignal.asReadonly();
  public unreadCount = this.unreadCountSignal.asReadonly();
  
  // Computed signal para notificaciones recientes (últimas 10)
  public recentNotifications = computed(() => 
    this.notificationsSignal().slice(0, 10)
  );

  constructor(private http: HttpClient) {}

  /**
   * Obtiene la lista de notificaciones del usuario con paginación
   */
  getNotifications(pageNumber: number = 1, pageSize: number = 20, isRead?: boolean): Observable<NotificationListResponse> {
    let url = `${this.apiUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (isRead !== undefined) {
      url += `&isRead=${isRead}`;
    }
    return this.http.get<NotificationListResponse>(url).pipe(
      tap((response: any) => {
        // Actualizar el signal con las notificaciones
        let notifications: Notification[] = [];
        if (Array.isArray(response)) {
          notifications = response;
        } else if (response.success && response.data) {
          notifications = Array.isArray(response.data) ? response.data : response.data.items;
        }
        this.notificationsSignal.set(notifications);
        
        // Actualizar contador de no leídas
        const unreadCount = notifications.filter(n => !n.isRead).length;
        this.unreadCountSignal.set(unreadCount);
      })
    );
  }

  /**
   * Obtiene el conteo de notificaciones no leídas
   */
  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>(`${this.apiUrl}/unread-count`).pipe(
      tap(response => {
        if (response.success) {
          this.unreadCountSignal.set(response.data);
        }
      })
    );
  }

  /**
   * Marca una notificación como leída
   */
  markAsRead(notificationId: string): Observable<MarkAsReadResponse> {
    return this.http.post<MarkAsReadResponse>(
      `${this.apiUrl}/${notificationId}/read`,
      {}
    ).pipe(
      tap((response: any) => {
        // Manejar tanto {success: true} como respuesta vacía
        if (response === null || response === undefined || response.success !== false) {
          // Verificar si la notificación existe en el signal
          const notificationExists = this.notificationsSignal().find(n => n.id === notificationId);
          
          if (notificationExists && !notificationExists.isRead) {
            // Actualizar el estado isRead en el signal
            this.notificationsSignal.update(notifications => 
              notifications.map(n => 
                n.id === notificationId ? { ...n, isRead: true, readAt: new Date() } : n
              )
            );
            
            // Decrementar el conteo de no leídas
            this.unreadCountSignal.update(count => Math.max(0, count - 1));
          } else if (!notificationExists) {
            // Si la notificación no está en el signal, solo decrementar el contador
            this.unreadCountSignal.update(count => Math.max(0, count - 1));
          }
        }
      })
    );
  }

  /**
   * Agrega una nueva notificación al inicio de la lista
   */
  addNotification(notification: Notification): void {
    this.notificationsSignal.update(notifications => [notification, ...notifications]);
    if (!notification.isRead) {
      this.unreadCountSignal.update(count => count + 1);
    }
  }

  /**
   * Actualiza el conteo de notificaciones no leídas manualmente
   */
  updateUnreadCount(count: number): void {
    this.unreadCountSignal.set(count);
  }

  /**
   * Incrementa el conteo de notificaciones no leídas
   */
  incrementUnreadCount(): void {
    this.unreadCountSignal.update(count => count + 1);
  }

  /**
   * Decrementa el conteo de notificaciones no leídas
   */
  decrementUnreadCount(): void {
    this.unreadCountSignal.update(count => Math.max(0, count - 1));
  }

  /**
   * Obtiene el valor actual del conteo de no leídas
   */
  getCurrentUnreadCount(): number {
    return this.unreadCountSignal();
  }
}
