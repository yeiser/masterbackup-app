import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/components/login.component';
import { RegisterComponent } from './features/register/components/register.component';
import { DashboardComponent } from './features/dashboard/components/dashboard-page/dashboard.component';
import { ResetPasswordComponent } from './features/reset-password/components/reset-password.component';
import { ForgotPasswordComponent } from './features/forgot-password/components/forgot-password.component';
import { DatabasesComponent } from './features/databases/components/databases.component';
import { WorkersComponent } from './features/workers/component/workers/workers.component';
import { WrapperComponent } from './layout/components/wrapper/wrapper.component';
import { authGuard } from './core/guards/auth.guard';
import { BackupScheduleListComponent } from './features/backup-schedule/components/backup-schedule-list/backup-schedule-list.component';
import { BackupExecutionListComponent } from './features/backup-execution/components/backup-execution-list/backup-execution-list.component';
import { NotificationsPageComponent } from './features/notifications/notifications-page.component';
import { ProfileComponent } from './features/profile/components/profile-page/profile.component';
import { SubscriptionDetailComponent } from './features/subscription/components/subscription-detail/subscription-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  
  // Rutas de autenticación (sin layout)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  
  // Rutas protegidas con layout
  {
    path: '',
    component: WrapperComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'databases', component: DatabasesComponent },
      { path: 'agents', component: WorkersComponent },
      { path: 'triggers', component: BackupScheduleListComponent },
      { path: 'activity', component: BackupExecutionListComponent },
      { path: 'notifications', component: NotificationsPageComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'subscriptions', component: SubscriptionDetailComponent }
      // Agregar más rutas aquí
    ]
  }
];
