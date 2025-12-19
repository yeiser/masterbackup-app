import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../../core/services/user.service';
import { StorageService } from '../../../../core/services/storage.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-security-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './security-tab.component.html',
  styleUrl: './security-tab.component.css'
})
export class SecurityTabComponent implements OnInit {
  verifyPasswordForm: FormGroup;
  changePasswordForm: FormGroup;
  
  isVerified = false;
  isVerifying = false;
  isChangingPassword = false;
  isToggling2FA = false;
  
  twoFactorEnabled = false;
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private storageService: StorageService
  ) {
    // Formulario para verificar contraseña actual
    this.verifyPasswordForm = this.fb.group({
      currentPassword: ['', Validators.required]
    });

    // Formulario para cambiar contraseña
    this.changePasswordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    this.load2FAStatus();
  }

  /**
   * Cargar estado de 2FA del usuario desde el API
   */
  load2FAStatus(): void {
    this.userService.getProfile().subscribe({
      next: (response) => {
        this.twoFactorEnabled = response.twoFactorEnabled;
        
        // Actualizar en storage también
        const currentUser = this.storageService.getCurrentUser();
        if (currentUser) {
          const updatedUser = {
            ...currentUser,
            twoFactorEnabled: response.twoFactorEnabled
          };
          this.storageService.setCurrentUser(updatedUser);
        }
      },
      error: (error) => {
        console.error('Error loading 2FA status:', error);
        // Fallback a storage si falla el API
        const currentUser = this.storageService.getCurrentUser();
        this.twoFactorEnabled = currentUser?.twoFactorEnabled || false;
      }
    });
  }

  /**
   * Validador personalizado para confirmar que las contraseñas coinciden
   */
  passwordMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');
    
    if (newPassword && confirmPassword && newPassword.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    
    return null;
  }

  /**
   * Verificar contraseña actual
   */
  verifyCurrentPassword(): void {
    if (this.verifyPasswordForm.invalid) {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'Por favor ingresa tu contraseña actual',
        confirmButtonColor: '#009ef7'
      });
      return;
    }

    this.isVerifying = true;
    const password = this.verifyPasswordForm.get('currentPassword')?.value;

    this.userService.verifyPassword(password).subscribe({
      next: (response) => {
        if (response.valid) {
          this.isVerified = true;
          this.changePasswordForm.patchValue({
            currentPassword: password
          });
          
          // Recargar estado de 2FA al verificar
          this.load2FAStatus();
          
          Swal.fire({
            icon: 'success',
            title: '¡Verificado!',
            text: 'Contraseña verificada correctamente',
            confirmButtonColor: '#009ef7',
            timer: 2000
          });
        } else {
          Swal.fire({
            icon: 'error',
            title: 'Error',
            text: 'La contraseña ingresada no es correcta',
            confirmButtonColor: '#009ef7'
          });
        }
        this.isVerifying = false;
      },
      error: (error) => {
        this.isVerifying = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo verificar la contraseña. Intenta nuevamente.',
          confirmButtonColor: '#009ef7'
        });
      }
    });
  }

  /**
   * Cambiar contraseña
   */
  changePassword(): void {
    // Marcar todos los campos como tocados
    Object.keys(this.changePasswordForm.controls).forEach(key => {
      this.changePasswordForm.get(key)?.markAsTouched();
    });

    if (this.changePasswordForm.invalid) {
      Swal.fire({
        icon: 'error',
        title: 'Error de Validación',
        text: 'Por favor completa todos los campos correctamente',
        confirmButtonColor: '#009ef7'
      });
      return;
    }

    this.isChangingPassword = true;
    const formData = this.changePasswordForm.value;

    this.userService.changePassword({
      currentPassword: formData.currentPassword,
      newPassword: formData.newPassword
    }).subscribe({
      next: () => {
        this.changePasswordForm.reset();
        this.changePasswordForm.patchValue({
          currentPassword: this.verifyPasswordForm.get('currentPassword')?.value
        });
        
        this.isChangingPassword = false;
        
        Swal.fire({
          icon: 'success',
          title: '¡Actualizado!',
          text: 'Tu contraseña ha sido cambiada exitosamente',
          confirmButtonColor: '#009ef7',
          timer: 2000
        });
      },
      error: (error) => {
        this.isChangingPassword = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo cambiar la contraseña. Intenta nuevamente.',
          confirmButtonColor: '#009ef7'
        });
      }
    });
  }

  /**
   * Alternar autenticación de dos factores
   */
  toggle2FA(): void {
    const action = this.twoFactorEnabled ? 'desactivar' : 'activar';
    
    Swal.fire({
      title: `¿${action.charAt(0).toUpperCase() + action.slice(1)} 2FA?`,
      text: `¿Estás seguro de que deseas ${action} la autenticación de dos factores?`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: `Sí, ${action}`,
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#009ef7',
      cancelButtonColor: '#f1416c'
    }).then((result) => {
      if (result.isConfirmed) {
        this.perform2FAToggle();
      }
    });
  }

  /**
   * Ejecutar el toggle de 2FA
   */
  perform2FAToggle(): void {
    this.isToggling2FA = true;
    
    this.userService.toggle2FA(!this.twoFactorEnabled).subscribe({
      next: (response) => {
        this.twoFactorEnabled = response.enabled;
        
        // Actualizar en storage
        const currentUser = this.storageService.getCurrentUser();
        if (currentUser) {
          const updatedUser = {
            ...currentUser,
            twoFactorEnabled: response.enabled
          };
          this.storageService.setCurrentUser(updatedUser);
        }
        
        this.isToggling2FA = false;
        
        Swal.fire({
          icon: 'success',
          title: '¡Actualizado!',
          text: response.message || `Autenticación de dos factores ${response.enabled ? 'activada' : 'desactivada'} exitosamente`,
          confirmButtonColor: '#009ef7',
          timer: 2000
        });
      },
      error: (error) => {
        this.isToggling2FA = false;
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo actualizar la configuración. Intenta nuevamente.',
          confirmButtonColor: '#009ef7'
        });
      }
    });
  }

  /**
   * Resetear verificación
   */
  resetVerification(): void {
    this.isVerified = false;
    this.verifyPasswordForm.reset();
    this.changePasswordForm.reset();
  }
}
