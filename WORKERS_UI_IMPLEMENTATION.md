# Workers UI Implementation

## Overview

Complete implementation of the Workers management UI in Angular, following the same architectural pattern as the Database Connections feature. This UI allows administrators to monitor and manage worker instances that process backup jobs.

## Architecture

### Components Structure

```
features/workers/
├── component/
│   └── workers/
│       ├── workers.component.ts        # Component logic (287 lines)
│       ├── workers.component.html      # UI template (287 lines)
│       └── workers.component.css       # Styles (100 lines)
├── models/
│   └── worker.models.ts                # TypeScript interfaces and enums
└── services/
    └── worker.service.ts               # HTTP service for Workers API
```

## Features Implemented

### 1. Real-time Monitoring
- **Auto-polling**: Refreshes worker list every 5 seconds
- **Status tracking**: Online, Offline, Busy, Error states
- **Heartbeat monitoring**: Color-coded health indicators
  - 🟢 Green: Last heartbeat < 30 seconds (Healthy)
  - 🟡 Yellow: Last heartbeat 30-60 seconds (Warning)
  - 🔴 Red: Last heartbeat > 60 seconds (Critical)

### 2. Dashboard Statistics
Four metric cards showing:
- **Total Workers**: Count of all registered workers
- **En Línea**: Currently online workers
- **Ocupados**: Workers processing jobs
- **Backups Procesados**: Total backups across all workers

### 3. Worker Details Display
Each worker card shows:
- **Identity**: Name, status badge, active jobs count
- **System Info**: Hostname, IP address, OS, version
- **Capabilities**: Supported database types (badges)
- **Tags**: Custom labels for organization
- **Statistics**: Total backups processed, bytes transferred
- **Health**: Last heartbeat with relative time

### 4. Search and Filter
- Real-time search bar
- Filters workers by name, hostname, or IP address
- Instant results without API calls

### 5. Worker Management
- **Activate/Deactivate**: Toggle switch on each card
- **Actions menu**: Dropdown with additional options
  - View Details (placeholder for future modal)
  - Toggle active status

### 6. Alert System
- Success alerts (green): Operation completed
- Error alerts (red): Operation failed
- Auto-dismiss after 5 seconds
- Manual dismiss button

## TypeScript Models

### WorkerStatus Enum
```typescript
enum WorkerStatus {
  Online = 'Online',
  Offline = 'Offline',
  Busy = 'Busy',
  Error = 'Error'
}
```

### WorkerDto Interface
```typescript
interface WorkerDto {
  id: string;
  tenantId: string;
  name: string;
  hostname?: string;
  ipAddress?: string;
  version?: string;
  osInfo?: string;
  status: WorkerStatus;
  lastHeartbeat: Date;
  isActive: boolean;
  supportedDatabaseTypes: string[];
  maxConcurrentJobs: number;
  currentActiveJobs: number;
  totalBackupsProcessed: number;
  totalBytesProcessed: number;
  tags?: string[];
  description?: string;
  registeredAt: Date;
  lastModified: Date;
}
```

### WorkerStatsDto Interface
```typescript
interface WorkerStatsDto {
  totalWorkers: number;
  onlineWorkers: number;
  offlineWorkers: number;
  busyWorkers: number;
  errorWorkers: number;
  totalBackupsProcessed: number;
}
```

## HTTP Service Methods

### WorkerService
```typescript
class WorkerService {
  // Get all workers for current tenant
  getAllWorkers(): Observable<WorkerDto[]>
  
  // Get specific worker by ID
  getWorkerById(id: string): Observable<WorkerDto>
  
  // Get dashboard statistics
  getWorkerStats(): Observable<WorkerStatsDto>
  
  // Deactivate a worker
  deactivateWorker(id: string): Observable<void>
  
  // Activate a worker
  activateWorker(id: string): Observable<void>
}
```

## Component Logic

### Lifecycle Hooks
- **ngOnInit()**: Loads workers, stats, starts polling
- **ngOnDestroy()**: Stops polling, cleanup

### Key Methods

#### Data Loading
```typescript
loadWorkers(): void {
  // Fetches all workers from API
  // Updates filteredWorkers array
  // Shows error alert on failure
}

loadStats(): void {
  // Fetches dashboard statistics
  // Updates stats object
  // Silent failures (no alert)
}
```

#### Polling Mechanism
```typescript
startPolling(): void {
  // Creates interval observable (5 seconds)
  // Calls loadWorkers() and loadStats()
  // Stores subscription for cleanup
}

stopPolling(): void {
  // Unsubscribes from interval
  // Prevents memory leaks
}
```

#### Worker Management
```typescript
toggleActive(worker: WorkerDto): void {
  // Calls activate or deactivate API
  // Shows success/error alert
  // Reloads data on success
}
```

#### Utility Functions
```typescript
getHeartbeatStatus(lastHeartbeat: Date): 'recent' | 'warning' | 'critical' {
  // Returns status based on time since last heartbeat
  // < 30s: recent (green)
  // 30-60s: warning (yellow)
  // > 60s: critical (red)
}

formatBytes(bytes: number): string {
  // Converts bytes to human-readable format
  // Returns: "1.23 GB", "456.78 MB", etc.
}
```

## UI Components

### Stats Dashboard
```html
<div class="row g-5 g-xl-8 mb-5">
  <!-- 4 metric cards with icons, values, labels -->
</div>
```

### Search Bar
```html
<input 
  type="text" 
  [(ngModel)]="searchTerm"
  (input)="applyFilter()"
  placeholder="Buscar workers..."
/>
```

### Worker Card
```html
<div class="card mb-5 mb-xl-10">
  <div class="card-body pt-9 pb-0">
    <!-- Server icon with status indicator -->
    <!-- Worker name + status badge + active jobs -->
    <!-- System info (hostname, IP, OS, version) -->
    <!-- Supported databases badges -->
    <!-- Tags badges -->
    <!-- Last heartbeat with color coding -->
    <!-- Statistics (backups, bytes) -->
    <!-- Active/Inactive toggle -->
    <!-- Actions menu -->
  </div>
</div>
```

### Status Badges
```typescript
getStatusBadgeClass(status: WorkerStatus): string {
  const classes = {
    'Online': 'badge-success',
    'Offline': 'badge-secondary',
    'Busy': 'badge-warning',
    'Error': 'badge-danger'
  };
  return `badge ${classes[status]}`;
}
```

## Routing Configuration

### app.routes.ts
```typescript
{
  path: '',
  component: WrapperComponent,
  canActivate: [authGuard],
  children: [
    { path: 'dashboard', component: DashboardComponent },
    { path: 'databases', component: DatabasesComponent },
    { path: 'workers', component: WorkersComponent } // ← New route
  ]
}
```

### Sidebar Menu
```html
<div class="menu-item">
  <a class="menu-link" routerLink="/workers" routerLinkActive="active">
    <span class="menu-icon">
      <i class="fa fa-server"></i>
    </span>
    <span class="menu-title">Workers</span>
  </a>
</div>
```

## CSS Styling

### Key Styles
- **Alert animations**: Slide down effect
- **Card hover effects**: Lift and shadow on hover
- **Status pulse**: Animated status indicators
- **Responsive design**: Mobile-friendly breakpoints
- **Toggle switches**: Custom styled form controls

### Responsive Breakpoints
```css
@media (max-width: 991.98px) {
  /* Reduced padding, smaller icons, adjusted font sizes */
}
```

## Integration with Backend API

### API Endpoints Used
- `GET /api/workers` - List all workers
- `GET /api/workers/{id}` - Get worker by ID
- `GET /api/workers/stats` - Get statistics
- `POST /api/workers/{id}/activate` - Activate worker
- `POST /api/workers/{id}/deactivate` - Deactivate worker

### Authentication
All requests include JWT token from localStorage:
```typescript
private getHeaders(): HttpHeaders {
  const token = localStorage.getItem('token');
  return new HttpHeaders({
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  });
}
```

## Error Handling

### Service Level
```typescript
catchError((error) => {
  console.error('Error fetching workers:', error);
  return throwError(() => error);
});
```

### Component Level
```typescript
this.workerService.getAllWorkers().subscribe({
  next: (data) => { /* success */ },
  error: (error) => {
    this.showAlertMessage('Error al cargar workers', 'danger');
  }
});
```

## Performance Optimizations

1. **Polling interval**: 5 seconds (balance between real-time and load)
2. **Silent stats loading**: No loading spinner for background updates
3. **Client-side filtering**: Search without API calls
4. **Cleanup on destroy**: Prevents memory leaks
5. **Single API call**: Filters applied in-memory

## Accessibility Features

- Semantic HTML structure
- ARIA labels on interactive elements
- Keyboard navigation support
- Screen reader friendly badges
- Focus indicators on form controls

## Future Enhancements

### Planned Features
1. **Worker Details Modal**: Full worker information view
2. **Real-time Logs**: WebSocket connection for live logs
3. **Performance Graphs**: Charts for metrics over time
4. **Bulk Operations**: Multi-select for batch actions
5. **Configuration Editor**: Modify worker settings
6. **Alert History**: View past worker alerts/events
7. **Export Reports**: Download worker statistics

### Potential Improvements
- **WebSocket integration**: Replace polling with real-time updates
- **Pagination**: Handle large worker counts
- **Advanced filters**: Filter by status, tags, capabilities
- **Sort options**: Order by name, status, last heartbeat
- **Drag-and-drop**: Assign workers to specific jobs

## Testing Checklist

### Manual Testing
- [ ] Workers load on page visit
- [ ] Polling updates data every 5 seconds
- [ ] Search filters workers in real-time
- [ ] Status badges show correct colors
- [ ] Heartbeat colors match time ranges
- [ ] Activate/deactivate toggle works
- [ ] Success/error alerts appear correctly
- [ ] Alerts auto-dismiss after 5 seconds
- [ ] Responsive design works on mobile
- [ ] Navigation from sidebar works
- [ ] AuthGuard prevents unauthorized access

### Integration Testing
- [ ] API calls include JWT token
- [ ] 401 responses redirect to login
- [ ] Network errors show user-friendly messages
- [ ] Concurrent API calls don't conflict
- [ ] Polling stops on component destroy

## Dependencies

### Required Imports
```typescript
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RelativeTimePipe } from 'shared/pipes/relative-time.pipe';
```

### External Libraries
- **RxJS**: Observables, intervals, subscriptions
- **Font Awesome**: Icons
- **Bootstrap 5**: Layout and components
- **Metronic Theme**: Styling framework

## Deployment Notes

### Build Command
```bash
ng build --configuration production
```

### Environment Variables
```typescript
// src/environments/environment.ts
export const environment = {
  apiUrl: 'http://localhost:5000/api', // Change for production
  production: false
};
```

### Production Checklist
- [ ] Update environment.prod.ts with production API URL
- [ ] Test with production backend
- [ ] Verify JWT token handling
- [ ] Check error logging
- [ ] Validate performance with large datasets
- [ ] Test across different browsers

## Documentation Links

- [Backend Workers API](../MasterBackup-API/Presentation/Controllers/WorkersController.cs)
- [Worker Entity](../MasterBackup-API/Domain/Entities/Worker.cs)
- [Worker Registration](../MasterBackup-Worker/Services/WorkerRegistrationService.cs)
- [Database Connections UI](./FRONTEND_TEST_CONNECTION_IMPLEMENTATION.md) (Similar pattern)

---

**Implementation Date**: 2024
**Status**: ✅ Complete
**Version**: 1.0.0
