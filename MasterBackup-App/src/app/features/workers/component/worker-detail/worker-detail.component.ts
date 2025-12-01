import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WorkerDto, WorkerStatusLabels, WorkerStatusColors, WorkerStatusIcons } from '../../models/worker.models';
import { WorkerService } from '../../services/worker.service';
import { RelativeTimePipe } from '../../../../core/pipes/relative-time.pipe';

@Component({
  selector: 'app-worker-detail',
  standalone: true,
  imports: [CommonModule, RelativeTimePipe],
  templateUrl: './worker-detail.component.html',
  styleUrl: './worker-detail.component.css'
})
export class WorkerDetailComponent implements OnInit {
  @Input() workerId?: string;
  @Output() close = new EventEmitter<void>();
  
  worker?: WorkerDto;
  isLoading = false;
  error?: string;

  // Enums para el template
  WorkerStatusLabels = WorkerStatusLabels;
  WorkerStatusColors = WorkerStatusColors;
  WorkerStatusIcons = WorkerStatusIcons;

  constructor(private workerService: WorkerService) {}

  ngOnInit(): void {
    if (this.workerId) {
      this.loadWorkerDetails();
    }
  }

  loadWorkerDetails(): void {
    if (!this.workerId) return;

    this.isLoading = true;
    this.error = undefined;

    this.workerService.getWorkerById(this.workerId).subscribe({
      next: (worker) => {
        this.worker = worker;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error cargando detalles del worker:', error);
        this.error = 'Error al cargar los detalles del worker';
        this.isLoading = false;
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    return `badge-light-${WorkerStatusColors[status as keyof typeof WorkerStatusColors]}`;
  }

  getHeartbeatStatus(lastHeartbeat: string): 'recent' | 'warning' | 'critical' {
    const now = new Date();
    const heartbeatDate = new Date(lastHeartbeat);
    const diffSeconds = (now.getTime() - heartbeatDate.getTime()) / 1000;

    if (diffSeconds < 60) return 'recent';
    if (diffSeconds < 300) return 'warning';
    return 'critical';
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  onClose(): void {
    this.close.emit();
  }
}
