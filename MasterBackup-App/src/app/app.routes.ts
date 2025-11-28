import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/components/login.component';
import { RegisterComponent } from './features/register/components/register.component';
import { DashboardComponent } from './features/dashboard/components/dashboard/dashboard.component';
import { ResetPasswordComponent } from './features/reset-password/components/reset-password.component';
import { ForgotPasswordComponent } from './features/forgot-password/components/forgot-password.component';
import { WrapperComponent } from './layout/components/wrapper/wrapper.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  
  // Rutas de autenticación (sin layout)
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  
  // Rutas protegidas con layout
  {
    path: 'home',
    component: WrapperComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent }
      // Agregar más rutas aquí
    ]
  }
];
