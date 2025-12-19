import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BackupStartedEvent {
  eventType: string;
  jobId: string;
  tenantId: string;
  backupScheduleId: string;
  timestamp: Date;
  data: {
    databaseName: string;
    databaseType: string;
    scheduleName: string;
    estimatedDurationMinutes: number;
  };
}

export interface BackupCompletedEvent {
  eventType: string;
  jobId: string;
  tenantId: string;
  backupScheduleId: string;
  timestamp: Date;
  data: {
    databaseName: string;
    scheduleName: string;
    blobUrl: string;
    backupSizeMB: number;
    duration: string;
  };
}

export interface BackupFailedEvent {
  eventType: string;
  jobId: string;
  tenantId: string;
  backupScheduleId: string;
  timestamp: Date;
  data: {
    databaseName: string;
    scheduleName: string;
    errorMessage: string;
    errorCode: string;
  };
}

export interface BackupProgressEvent {
  eventType: string;
  jobId: string;
  tenantId: string;
  backupScheduleId: string;
  timestamp: Date;
  data: {
    databaseName: string;
    progressPercentage: number;
    currentStep: string;
    processedBytes?: number;
    totalBytes?: number;
  };
}

export interface NewNotificationEvent {
  id: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  redirectUrl?: string;
  isRead: boolean;
  createdAt: Date;
  expiresAt?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private isConnected = false;

  // Observables para cada tipo de evento
  public backupStarted$ = new Subject<BackupStartedEvent>();
  public backupCompleted$ = new Subject<BackupCompletedEvent>();
  public backupFailed$ = new Subject<BackupFailedEvent>();
  public backupProgress$ = new Subject<BackupProgressEvent>();
  public newNotification$ = new Subject<NewNotificationEvent>();
  public connectionStatus$ = new Subject<boolean>();

  constructor() {}

  /**
   * Inicia la conexión SignalR con el Hub de notificaciones
   */
  startConnection(token: string): Promise<void> {
    if (this.isConnected) {
      console.log('SignalR: Ya existe una conexión activa');
      return Promise.resolve();
    }

    // Obtener la URL base sin /api
    const baseUrl = environment.apiUrl.replace('/api', '');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/notificationHub`, {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Reintentos: 0s, 2s, 10s, 30s, luego cada 30s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Configurar listeners para los eventos
    this.registerEventHandlers();

    // Configurar eventos de conexión
    this.hubConnection.onreconnecting(() => {
      console.log('SignalR: Reconectando...');
      this.isConnected = false;
      this.connectionStatus$.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('SignalR: Reconectado exitosamente');
      this.isConnected = true;
      this.connectionStatus$.next(true);
    });

    this.hubConnection.onclose((error) => {
      console.error('SignalR: Conexión cerrada', error);
      this.isConnected = false;
      this.connectionStatus$.next(false);
    });

    // Iniciar conexión
    return this.hubConnection
      .start()
      .then(() => {
        console.log('✓ SignalR: Conexión establecida exitosamente');
        console.log('✓ SignalR: Hub URL:', `${baseUrl}/notificationHub`);
        this.isConnected = true;
        this.connectionStatus$.next(true);
        
        // Verificar conexión
        this.hubConnection?.invoke('GetConnectionInfo')
          .then((info: any) => {
            console.log('✓ SignalR: Info de conexión:', info);
          })
          .catch((err) => {
            console.warn('⚠ SignalR: No se pudo obtener info de conexión', err);
          });
      })
      .catch((err) => {
        console.error('✗ SignalR: Error al conectar', err);
        this.isConnected = false;
        this.connectionStatus$.next(false);
        throw err;
      });
  }

  /**
   * Registra los handlers para los eventos de SignalR
   */
  private registerEventHandlers(): void {
    if (!this.hubConnection) return;

    // Evento: Backup iniciado
    this.hubConnection.on('BackupStarted', (data: BackupStartedEvent) => {
      console.log('SignalR: BackupStarted', data);
      this.backupStarted$.next(data);
    });

    // Evento: Backup completado
    this.hubConnection.on('BackupCompleted', (data: BackupCompletedEvent) => {
      console.log('SignalR: BackupCompleted', data);
      this.backupCompleted$.next(data);
    });

    // Evento: Backup fallido
    this.hubConnection.on('BackupFailed', (data: BackupFailedEvent) => {
      console.log('SignalR: BackupFailed', data);
      this.backupFailed$.next(data);
    });

    // Evento: Progreso del backup
    this.hubConnection.on('BackupProgress', (data: BackupProgressEvent) => {
      console.log('SignalR: BackupProgress', data);
      this.backupProgress$.next(data);
    });

    // Evento: Nueva notificación
    this.hubConnection.on('NewNotification', (data: NewNotificationEvent) => {
      console.log('✓ SignalR: NewNotification recibida', data);
      this.newNotification$.next(data);
    });

    console.log('✓ SignalR: Todos los event handlers registrados');
  }

  /**
   * Detiene la conexión SignalR
   */
  stopConnection(): Promise<void> {
    if (!this.hubConnection || !this.isConnected) {
      return Promise.resolve();
    }

    return this.hubConnection
      .stop()
      .then(() => {
        console.log('SignalR: Conexión cerrada');
        this.isConnected = false;
        this.connectionStatus$.next(false);
      })
      .catch((err) => {
        console.error('SignalR: Error al cerrar conexión', err);
        throw err;
      });
  }

  /**
   * Verifica si hay una conexión activa
   */
  isConnectionActive(): boolean {
    return this.isConnected && 
           this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Obtiene el estado actual de la conexión
   */
  getConnectionState(): signalR.HubConnectionState | undefined {
    return this.hubConnection?.state;
  }
}
