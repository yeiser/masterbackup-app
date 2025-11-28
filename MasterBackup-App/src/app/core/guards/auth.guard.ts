import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { StorageService } from '../services/storage.service';

export const authGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const storageService = inject(StorageService);
  const token = storageService.getAuthToken();

  if (token) {
    // Usuario autenticado, permitir acceso
    return true;
  }

  // Usuario no autenticado, redirigir a login
  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
