import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StorageService } from '../../../../core/services/storage.service';
import { SubscriptionService } from '../../../../core/services/subscription.service';
import { BackupStatisticsService } from '../../../../core/services/backup-statistics.service';
import { SubscriptionInfo } from '../../../../core/models/subscription.model';
import { BackupStatistics } from '../../../../core/models/backup-statistics.model';
import { BackupHistoryService } from '../../../backup-execution/services/backup-history.service';
import { BackupHistoryDto, BackupStatus } from '../../../backup-execution/models/backup-history.models';

declare var ApexCharts: any;

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, AfterViewInit {
  userName: string = 'Usuario';
  subscriptionInfo: SubscriptionInfo | null = null;
  storagePercentage: number = 0;
  backupStats: BackupStatistics | null = null;
  
  // Backup counts
  pendingCount: number = 0;
  inProgressCount: number = 0;
  failedCount: number = 0;
  completedCount: number = 0;
  
  // Recent backups
  recentBackups: BackupHistoryDto[] = [];
  BackupStatus = BackupStatus;
  
  constructor(
    private storageService: StorageService,
    private subscriptionService: SubscriptionService,
    private backupStatisticsService: BackupStatisticsService,
    private backupHistoryService: BackupHistoryService
  ) {}

  ngOnInit(): void {
    // Obtener datos del usuario del StorageService
    const user = this.storageService.getCurrentUser();
    if (user) {
      this.userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Usuario';
    }

    // Obtener información de la suscripción
    this.loadSubscriptionInfo();
    
    // Obtener estadísticas de backups
    this.loadBackupStatistics();
    
    // Obtener backups recientes
    this.loadRecentBackups();
  }

  ngAfterViewInit(): void {
    // El gráfico se inicializará después de cargar los datos
  }

  loadSubscriptionInfo(): void {
    this.subscriptionService.getCurrentSubscription().subscribe({
      next: (data) => {
        this.subscriptionInfo = data;
        this.storagePercentage = data.usage.storagePercentage;
        // Inicializar el gráfico después de cargar los datos
        setTimeout(() => this.initStorageChart(), 100);
      },
      error: (error) => {
        console.error('Error loading subscription info:', error);
        // Mostrar gráfico con 0% si hay error
        this.storagePercentage = 0;
        setTimeout(() => this.initStorageChart(), 100);
      }
    });
  }

  initStorageChart(): void {
    const element = document.querySelector('.mixed-widget-4-chart');
    if (!element) {
      return;
    }

    const color = element.getAttribute('data-kt-chart-color') || 'primary';
    const height = parseInt(element.getAttribute('style')?.match(/height:\s*(\d+)px/)?.[1] || '225');

    const options = {
      series: [this.storagePercentage],
      chart: {
        fontFamily: 'inherit',
        height: height,
        type: 'radialBar',
      },
      plotOptions: {
        radialBar: {
          hollow: {
            margin: 0,
            size: '65%',
          },
          dataLabels: {
            name: {
              show: false,
              fontWeight: '700',
            },
            value: {
              color: '#5E6278',
              fontSize: '30px',
              fontWeight: '700',
              offsetY: 12,
              show: true,
              formatter: (val: number) => {
                return val + '%';
              },
            },
          },
          track: {
            background: '#00000020',
            strokeWidth: '100%',
          },
        },
      },
      colors: [this.getChartColor(color)],
      stroke: {
        lineCap: 'round',
      },
      labels: ['Storage'],
    };

    const chart = new ApexCharts(element, options);
    chart.render();
  }

  getChartColor(color: string): string {
    const colors: { [key: string]: string } = {
      primary: '#3E97FF',
      success: '#50CD89',
      info: '#7239EA',
      warning: '#FFC700',
      danger: '#F1416C',
    };
    return colors[color] || colors['primary'];
  }

  getStorageUsedGB(): string {
    return this.subscriptionInfo?.usage.storageUsedGB.toFixed(2) || '0';
  }

  getMaxStorageGB(): string {
    return this.subscriptionInfo?.limits.maxStorageGB.toString() || '0';
  }

  loadBackupStatistics(): void {
    this.backupStatisticsService.getOverallStatistics().subscribe({
      next: (stats) => {
        this.backupStats = stats;
        
        // Extract counts from status breakdown
        stats.statusBreakdown.forEach(breakdown => {
          switch(breakdown.status) {
            case 1: // Pending
              this.pendingCount = breakdown.count;
              break;
            case 2: // InProgress
              this.inProgressCount = breakdown.count;
              break;
            case 4: // Failed
              this.failedCount = breakdown.count;
              break;
            case 3: // Completed
              this.completedCount = breakdown.count;
              break;
          }
        });

        // Initialize bar chart after data is loaded
        setTimeout(() => {
          this.initBackupChart();
        }, 100);
      },
      error: (error) => {
        console.error('Error loading backup statistics:', error);
        // Keep default values (0) on error
      }
    });
  }

  initBackupChart(): void {
    const chartElement = document.querySelector('#backup-execution-chart');
    if (!chartElement) {
      return;
    }

    const options: any = {
      series: [{
        name: 'Backups',
        data: [this.pendingCount, this.inProgressCount, this.failedCount, this.completedCount]
      }],
      chart: {
        type: 'bar',
        height: 200,
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '55%',
          borderRadius: 5,
          dataLabels: {
            position: 'top'
          }
        }
      },
      dataLabels: {
        enabled: true,
        offsetY: -20,
        style: {
          fontSize: '14px',
          colors: ['#ffffff'],
          fontWeight: 'bold'
        }
      },
      colors: ['#920404ff'],
      xaxis: {
        categories: ['Pendiente', 'En Progreso', 'Fallido', 'Completado'],
        labels: {
          show: false
        }
      },
      yaxis: {
        labels: {
          style: {
            colors: '#ffffff',
            fontSize: '12px'
          }
        }
      },
      grid: {
        borderColor: 'rgba(255, 255, 255, 0.2)',
        strokeDashArray: 4,
        yaxis: {
          lines: {
            show: true
          }
        }
      },
      tooltip: {
        y: {
          formatter: (val: number) => {
            return val + ' backups';
          }
        }
      }
    };

    const chart = new ApexCharts(chartElement, options);
    chart.render();
  }

  loadRecentBackups(): void {
    this.backupHistoryService.getBackupHistory({
      pageNumber: 1,
      pageSize: 7,
      sortBy: 'StartTime',
      sortDirection: 'DESC'
    }).subscribe({
      next: (result) => {
        this.recentBackups = result.items;
      },
      error: (error) => {
        console.error('Error loading recent backups:', error);
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'badge-light-warning';
      case 'InProgress':
        return 'badge-light-primary';
      case 'Completed':
        return 'badge-light-success';
      case 'Failed':
        return 'badge-light-danger';
      default:
        return 'badge-light-secondary';
    }
  }

  getStatusText(status: BackupStatus): string {
    switch (status) {
      case BackupStatus.Pending:
        return 'Pendiente';
      case BackupStatus.InProgress:
        return 'En Progreso';
      case BackupStatus.Completed:
        return 'Completado';
      case BackupStatus.Failed:
        return 'Fallido';
      default:
        return 'Desconocido';
    }
  }

  getStatusIconClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'text-warning';
      case 'InProgress':
        return 'text-primary';
      case 'Completed':
        return 'text-success';
      case 'Failed':
        return 'text-danger';
      default:
        return 'text-secondary';
    }
  }

  formatDate(date: string | Date): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    
    const time = d.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
    
    if (d.toDateString() === today.toDateString()) {
      return 'Hoy\n' + time;
    } else if (d.toDateString() === yesterday.toDateString()) {
      return 'Ayer\n' + time;
    } else {
      return d.toLocaleDateString('es-ES', { day: '2-digit', month: 'short' }) + '\n' + time;
    }
  }
}
