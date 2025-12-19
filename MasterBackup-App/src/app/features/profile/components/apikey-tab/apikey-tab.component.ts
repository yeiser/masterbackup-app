import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TenantService } from '../../../../core/services/tenant.service';
import Swal from 'sweetalert2';

interface ApiKeyLog {
  id: string;
  remoteIp: string;
  success: boolean;
  errorMessage?: string;
  endpoint?: string;
  method?: string;
  createdAt: string;
}

interface ApiKeyLogsResponse {
  logs: ApiKeyLog[];
  totalCount: number;
  pageSize: number;
  currentPage: number;
  hasMore: boolean;
}

@Component({
  selector: 'app-apikey-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './apikey-tab.component.html',
  styleUrl: './apikey-tab.component.css'
})
export class ApikeyTabComponent implements OnInit {
  apiKey: string = '';
  logs: ApiKeyLog[] = [];
  isLoadingKey = false;
  isLoadingLogs = false;
  isLoadingMore = false;
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  hasMore = false;
  showApiKey = false;

  constructor(private tenantService: TenantService) {}

  ngOnInit(): void {
    this.loadApiKey();
    this.loadLogs();
  }

  /**
   * Cargar API Key del tenant
   */
  loadApiKey(): void {
    this.isLoadingKey = true;
    this.tenantService.getApiKey().subscribe({
      next: (response) => {
        this.apiKey = response.apiKey;
        this.isLoadingKey = false;
      },
      error: (error) => {
        console.error('Error loading API key:', error);
        this.isLoadingKey = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: 'No se pudo cargar la API key',
          confirmButtonColor: '#009ef7'
        });
      }
    });
  }

  /**
   * Cargar logs de conexiones
   */
  loadLogs(page: number = 1): void {
    if (page === 1) {
      this.isLoadingLogs = true;
    } else {
      this.isLoadingMore = true;
    }

    this.tenantService.getApiKeyLogs(page, this.pageSize).subscribe({
      next: (response: ApiKeyLogsResponse) => {
        if (page === 1) {
          this.logs = response.logs;
        } else {
          this.logs = [...this.logs, ...response.logs];
        }
        
        this.currentPage = response.currentPage;
        this.totalCount = response.totalCount;
        this.hasMore = response.hasMore;
        this.isLoadingLogs = false;
        this.isLoadingMore = false;
      },
      error: (error) => {
        console.error('Error loading logs:', error);
        this.isLoadingLogs = false;
        this.isLoadingMore = false;
      }
    });
  }

  /**
   * Cargar más logs (lazy loading)
   */
  loadMore(): void {
    if (this.hasMore && !this.isLoadingMore) {
      this.loadLogs(this.currentPage + 1);
    }
  }

  /**
   * Copiar API key al portapapeles
   */
  copyApiKey(): void {
    navigator.clipboard.writeText(this.apiKey).then(() => {
      Swal.fire({
        icon: 'success',
        title: '¡Copiado!',
        text: 'API Key copiada al portapapeles',
        confirmButtonColor: '#009ef7',
        timer: 2000
      });
    }).catch(() => {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se pudo copiar la API Key',
        confirmButtonColor: '#009ef7'
      });
    });
  }

  /**
   * Alternar visibilidad de la API key
   */
  toggleApiKeyVisibility(): void {
    this.showApiKey = !this.showApiKey;
  }

  /**
   * Obtener API key enmascarada
   */
  getMaskedApiKey(): string {
    if (!this.apiKey) return '';
    if (this.apiKey.length <= 12) return '•'.repeat(this.apiKey.length);
    return this.apiKey.substring(0, 8) + '•'.repeat(this.apiKey.length - 12) + this.apiKey.substring(this.apiKey.length - 4);
  }

  /**
   * Refrescar logs
   */
  refreshLogs(): void {
    this.loadLogs(1);
  }
}
