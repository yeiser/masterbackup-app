import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { StorageService } from '../../../../core/services/storage.service';
import { AuthService } from '../../../../core/services/auth.service';
import Swal from 'sweetalert2';
import { NavbarProfileComponent } from '../navbar-profile/navbar-profile.component';
import { InformationTabComponent } from '../information-tab/information-tab.component';
import { OrganizationTabComponent } from '../organization-tab/organization-tab.component';
import { SecurityTabComponent } from '../security-tab/security-tab.component';
import { ApikeyTabComponent } from '../apikey-tab/apikey-tab.component';

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

interface TenantInfo {
  id: string;
  name: string;
  subdomain?: string;
  isActive: boolean;
  createdAt?: Date;
  maxUsers?: number;
  currentUsers?: number;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NavbarProfileComponent, InformationTabComponent, OrganizationTabComponent, SecurityTabComponent, ApikeyTabComponent ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  user: UserProfile | null = null;
  tenant: TenantInfo | null = null;
  
  profileForm: FormGroup;
  passwordForm: FormGroup;
  tenantForm: FormGroup;
  
  activeTab: 'profile' | 'security' | 'tenant' | 'apikey' = 'profile';
  
  /**
   * Cambiar tab activo
   */
  onTabChange(tab: 'profile' | 'security' | 'tenant' | 'apikey'): void {
    this.activeTab = tab;
  }
  isLoadingProfile = false;
  isLoadingPassword = false;
  isLoadingTenant = false;
  
  // Alert
  alertVisible = false;
  alertMessage = '';
  alertType: 'success' | 'error' | 'info' = 'success';

  constructor(
    private fb: FormBuilder,
    private storageService: StorageService,
    private authService: AuthService
  ) {
    // Formulario de perfil
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['']
    });

    // Formulario de contraseña
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validator: this.passwordMatchValidator });

    // Formulario de tenant
    this.tenantForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      subdomain: ['']
    });
  }

  ngOnInit(): void {
    this.loadUserData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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

      // Cargar tenant info (mock data - reemplazar con API real)
      this.tenant = {
        id: currentUser.tenantId,
        name: currentUser.tenantName || 'Mi Organización',
        subdomain: currentUser.subdomain,
        isActive: true,
        createdAt: new Date(),
        maxUsers: 10,
        currentUsers: 1
      };

      // Llenar formularios
      this.profileForm.patchValue({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        email: this.user.email,
        phoneNumber: this.user.phoneNumber || ''
      });

      this.tenantForm.patchValue({
        name: this.tenant.name,
        subdomain: this.tenant.subdomain || ''
      });
    }
  }

  /**
   * Validador personalizado para confirmar contraseña
   */
  passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    
    if (newPassword !== confirmPassword) {
      return { passwordMismatch: true };
    }
    return null;
  }

  /**
   * Cambiar tab activo
   */
  setActiveTab(tab: 'profile' | 'security' | 'tenant'): void {
    this.activeTab = tab;
  }

  /**
   * Actualizar perfil
   */
  updateProfile(): void {
    if (this.profileForm.invalid) {
      this.showAlert('Por favor completa todos los campos requeridos', 'error');
      return;
    }

    this.isLoadingProfile = true;
    const formData = this.profileForm.value;

    // TODO: Llamar al servicio API real
    setTimeout(() => {
      // Actualizar usuario en storage
      const currentUser = this.storageService.getCurrentUser();
      const updatedUser = {
        ...currentUser,
        ...formData
      };
      
      this.storageService.setCurrentUser(updatedUser);
      this.user = { ...this.user!, ...formData };
      
      this.isLoadingProfile = false;
      this.showAlert('Perfil actualizado exitosamente', 'success');
    }, 1000);
  }

  /**
   * Cambiar contraseña
   */
  changePassword(): void {
    if (this.passwordForm.invalid) {
      if (this.passwordForm.errors?.['passwordMismatch']) {
        this.showAlert('Las contraseñas no coinciden', 'error');
      } else {
        this.showAlert('Por favor completa todos los campos', 'error');
      }
      return;
    }

    Swal.fire({
      title: '¿Cambiar Contraseña?',
      text: 'Se cerrará tu sesión y deberás iniciar sesión nuevamente',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, cambiar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#009ef7',
      cancelButtonColor: '#f1416c'
    }).then((result) => {
      if (result.isConfirmed) {
        this.isLoadingPassword = true;
        
        // TODO: Llamar al servicio API real
        setTimeout(() => {
          this.passwordForm.reset();
          this.isLoadingPassword = false;
          
          Swal.fire({
            title: 'Contraseña Actualizada',
            text: 'Tu contraseña ha sido cambiada exitosamente',
            icon: 'success',
            confirmButtonText: 'Entendido',
            confirmButtonColor: '#009ef7'
          });
        }, 1000);
      }
    });
  }

  /**
   * Actualizar información del tenant
   */
  updateTenant(): void {
    if (this.tenantForm.invalid) {
      this.showAlert('Por favor completa todos los campos requeridos', 'error');
      return;
    }

    this.isLoadingTenant = true;
    const formData = this.tenantForm.value;

    // TODO: Llamar al servicio API real
    setTimeout(() => {
      if (this.tenant) {
        this.tenant = { ...this.tenant, ...formData };
      }
      
      this.isLoadingTenant = false;
      this.showAlert('Información del tenant actualizada', 'success');
    }, 1000);
  }

  /**
   * Subir avatar
   */
  onAvatarChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      
      // Validar tamaño (max 2MB)
      if (file.size > 2 * 1024 * 1024) {
        this.showAlert('La imagen debe ser menor a 2MB', 'error');
        return;
      }

      // Validar tipo
      if (!file.type.startsWith('image/')) {
        this.showAlert('Solo se permiten imágenes', 'error');
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        if (this.user) {
          this.user.avatar = e.target.result;
          
          // TODO: Subir al servidor
          this.showAlert('Avatar actualizado', 'success');
        }
      };
      reader.readAsDataURL(file);
    }
  }

  /**
   * Eliminar avatar
   */
  removeAvatar(): void {
    if (this.user) {
      this.user.avatar = undefined;
      this.showAlert('Avatar eliminado', 'info');
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
   * Mostrar alerta
   */
  showAlert(message: string, type: 'success' | 'error' | 'info'): void {
    this.alertMessage = message;
    this.alertType = type;
    this.alertVisible = true;

    setTimeout(() => {
      this.alertVisible = false;
    }, 5000);
  }

  closeAlert(): void {
    this.alertVisible = false;
  }
}
