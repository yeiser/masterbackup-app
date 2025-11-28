import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/components/login.component';
import { RegisterComponent } from './features/register/components/register.component';
import { DashboardComponent } from './features/dashboard/components/dashboard.component';
import { ResetPasswordComponent } from './features/reset-password/components/reset-password.component';
import { ForgotPasswordComponent } from './features/forgot-password/components/forgot-password.component';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent }
  // Agregar más rutas aquí
];
