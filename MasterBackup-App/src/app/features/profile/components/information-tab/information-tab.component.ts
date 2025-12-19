import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { StorageService } from '../../../../core/services/storage.service';
import { UserService } from '../../../../core/services/user.service';
import Swal from 'sweetalert2';
import { CapitalizePipe } from '../../../../shared/pipes/capitalize.pipe';

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
  selector: 'app-information-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CapitalizePipe],
  templateUrl: './information-tab.component.html',
  styleUrl: './information-tab.component.css'
})
export class InformationTabComponent implements OnInit {
  user: UserProfile | null = null;
  editForm: FormGroup;
  showModal = false;
  isLoading = false;

  constructor(
    private storageService: StorageService,
    private userService: UserService,
    private fb: FormBuilder
  ) {
    this.editForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.maxLength(20)]]
    });
  }

  ngOnInit(): void {
    this.loadUserData();
  }

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

  getFullName(): string {
    if (!this.user) return '';
    return `${this.user.firstName} ${this.user.lastName}`;
  }

  openEditModal(): void {
    if (this.user) {
      this.editForm.patchValue({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        email: this.user.email,
        phoneNumber: this.user.phoneNumber || ''
      });
      this.showModal = true;
    }
  }

  closeModal(): void {
    this.showModal = false;
    this.editForm.reset();
  }

  saveChanges(): void {
    // Marcar todos los campos como tocados para mostrar validaciones
    Object.keys(this.editForm.controls).forEach(key => {
      this.editForm.get(key)?.markAsTouched();
    });

    if (this.editForm.invalid) {
      Swal.fire({
        icon: 'error',
        title: 'Error de Validación',
        text: 'Por favor completa todos los campos correctamente antes de guardar',
        confirmButtonColor: '#009ef7'
      });
      return;
    }

    this.isLoading = true;
    const formData = this.editForm.value;

    // Limpiar phoneNumber si está vacío para evitar error de validación
    const cleanedData = {
      firstName: formData.firstName,
      lastName: formData.lastName,
      email: formData.email,
      phoneNumber: formData.phoneNumber && formData.phoneNumber.trim() !== '' ? formData.phoneNumber : null
    };

    // Llamar al API para actualizar el perfil
    this.userService.updateProfile(cleanedData).subscribe({
      next: (response) => {
        // Actualizar usuario en storage
        const currentUser = this.storageService.getCurrentUser();
        const updatedUser = {
          ...currentUser,
          firstName: response.firstName,
          lastName: response.lastName,
          email: response.email,
          phoneNumber: response.phoneNumber
        };
        
        this.storageService.setCurrentUser(updatedUser);
        
        // Actualizar datos locales
        if (this.user) {
          this.user = {
            ...this.user,
            firstName: response.firstName,
            lastName: response.lastName,
            email: response.email,
            phoneNumber: response.phoneNumber
          };
        }
        
        this.isLoading = false;
        this.closeModal();
        
        Swal.fire({
          icon: 'success',
          title: '¡Actualizado!',
          text: 'Tu información personal ha sido actualizada exitosamente',
          confirmButtonColor: '#009ef7',
          timer: 2000
        });
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error updating profile:', error);
        
        Swal.fire({
          icon: 'error',
          title: 'Error',
          text: error.error?.message || 'No se pudo actualizar la información. Por favor intenta nuevamente.',
          confirmButtonColor: '#009ef7'
        });
      }
    });
  }
}
