import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StorageService } from '../../../../core/services/storage.service';
import { CapitalizePipe } from '../../../../core/pipes/capitalize.pipe';

interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: string;
  tenantName?: string;
  phoneNumber?: string;
  avatar?: string;
  createdAt?: Date;
  lastLogin?: Date;
}

@Component({
  selector: 'app-navbar-profile',
  standalone: true,
  imports: [CommonModule, CapitalizePipe],
  templateUrl: './navbar-profile.component.html',
  styleUrl: './navbar-profile.component.css'
})
export class NavbarProfileComponent implements OnInit {
  user: UserProfile | null = null;
  
  @Output() tabChange = new EventEmitter<'profile' | 'security' | 'tenant' | 'apikey'>();
  activeTab: 'profile' | 'security' | 'tenant' | 'apikey' = 'profile';

  constructor(private storageService: StorageService) {}

  ngOnInit(): void {
    this.loadUserData();
  }

  /**
   * Cargar datos del usuario
   */
  loadUserData(): void {
    const currentUser = this.storageService.getCurrentUser();
    
    if (currentUser) {
      this.user = {
        id: currentUser.id || currentUser.userId,
        email: currentUser.email,
        firstName: currentUser.firstName,
        lastName: currentUser.lastName,
        role: currentUser.role,
        tenantId: currentUser.tenantId,
        tenantName: currentUser.tenantName,
        phoneNumber: currentUser.phoneNumber,
        avatar: currentUser.avatar,
        createdAt: currentUser.createdAt ? new Date(currentUser.createdAt) : undefined,
        lastLogin: currentUser.lastLogin ? new Date(currentUser.lastLogin) : undefined
      };
    }
  }

  /**
   * Obtener iniciales del usuario
   */
  getUserInitials(): string {
    if (!this.user) return 'U';
    return `${this.user.firstName.charAt(0)}${this.user.lastName.charAt(0)}`.toUpperCase();
  }

  /**
   * Obtener nombre completo
   */
  getFullName(): string {
    if (!this.user) return '';
    return `${this.user.firstName} ${this.user.lastName}`;
  }

  /**
   * Obtener color del badge de rol
   */
  getRoleBadgeClass(): string {
    if (!this.user) return 'badge-light';
    
    switch (this.user.role.toLowerCase()) {
      case 'admin':
      case 'superadmin':
        return 'badge-light-danger';
      case 'user':
        return 'badge-light-primary';
      default:
        return 'badge-light';
    }
  }

  /**
   * Cambiar tab activo
   */
  selectTab(tab: 'profile' | 'security' | 'tenant' | 'apikey'): void {
    this.activeTab = tab;
    this.tabChange.emit(tab);
  }
}
