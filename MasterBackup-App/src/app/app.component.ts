import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { SignalRService } from './core/services/signalr.service';
import { StorageService } from './core/services/storage.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'MasterBackup';
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private signalRService: SignalRService,
    private storageService: StorageService
  ) {}

  ngOnInit(): void {
    // Verificar si hay usuario autenticado e iniciar SignalR
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (user) {
          this.initializeSignalR();
        } else {
          // Cerrar conexión si el usuario cierra sesión
          this.signalRService.stopConnection();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.signalRService.stopConnection();
  }

  /**
   * Inicializa la conexión SignalR con el token del usuario
   */
  private initializeSignalR(): void {
    const token = this.storageService.getAuthToken();
    
    if (token) {
      this.signalRService.startConnection(token)
        .then(() => {
          console.log('✓ SignalR: Inicializado correctamente en AppComponent');
        })
        .catch(err => {
          console.error('✗ SignalR: Error al inicializar en AppComponent', err);
          
          // Reintentar después de 5 segundos
          setTimeout(() => {
            if (this.storageService.getAuthToken()) {
              console.log('SignalR: Reintentando conexión...');
              this.initializeSignalR();
            }
          }, 5000);
        });
    }
  }
}
