import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { StorageService } from '../../../core/services/storage.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit {
  userName: string = 'Usuario';
  userRole: string = 'Admin';

  constructor(
    private router: Router,
    private storageService: StorageService
  ) {}

  ngOnInit(): void {
    const user = this.storageService.getCurrentUser();
    if (user) {
      this.userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'Usuario';
      this.userRole = user.role || 'Admin';
    }
  }

  logout(): void {
    // Limpiar todos los datos de sesión usando el servicio
    this.storageService.clearAuthToken();
    this.storageService.clearCurrentUser();
    this.storageService.clearApiKey();
    this.router.navigate(['/login']);
  }
}
