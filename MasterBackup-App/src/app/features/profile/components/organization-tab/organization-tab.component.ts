import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { StorageService } from '../../../../core/services/storage.service';
import { TenantService } from '../../../../core/services/tenant.service';
import Swal from 'sweetalert2';

interface TenantInfo {
  id: string;
  name: string;
  identificacion?: string;
  tipoId?: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  paginaWeb?: string;
  createdAt?: Date;
  isActive: boolean;
}

@Component({
  selector: 'app-organization-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './organization-tab.component.html',
  styleUrl: './organization-tab.component.css'
})
export class OrganizationTabComponent implements OnInit {
  tenant: TenantInfo | null = null;
  editForm: FormGroup;
  showModal = false;
  isLoading = false;

  // Tipos de identificación comunes en Colombia
  tiposId = [
    { value: 'CC', label: 'Cédula de Ciudadanía' },
    { value: 'CE', label: 'Cédula de Extranjería' },
    { value: 'NT', label: 'NIT' },
    { value: 'PA', label: 'Pasaporte' },
    { value: 'TI', label: 'Tarjeta de Identidad' }
  ];

  constructor(
    private storageService: StorageService,
    private tenantService: TenantService,
    private fb: FormBuilder
  ) {
    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      identificacion: ['', [Validators.maxLength(20)]],
      tipoId: ['', [Validators.maxLength(2)]],
      direccion: ['', [Validators.maxLength(50)]],
      telefono: ['', [Validators.maxLength(20)]],
      email: ['', [Validators.maxLength(50)]],
      paginaWeb: ['', [Validators.maxLength(50)]]
    });
  }

  ngOnInit(): void {
    this.loadTenantData();
  }

  loadTenantData(): void {
    // Llamar al API para obtener datos completos del tenant
    this.tenantService.getTenant().subscribe({
      next: (response) => {
        this.tenant = {
          id: response.id,
          name: response.name,
          identificacion: response.identificacion,
          tipoId: response.tipoId,
          direccion: response.direccion,
          telefono: response.telefono,
          email: response.email,
          paginaWeb: response.paginaWeb,
          createdAt: response.createdAt ? new Date(response.createdAt) : undefined,
          isActive: response.isActive
        };
      },
      error: (error) => {
        console.error('Error loading tenant data:', error);
        
        // Fallback: usar datos del storage si el API falla
        const currentUser = this.storageService.getCurrentUser();
        if (currentUser && currentUser.tenantId) {
          this.tenant = {
            id: currentUser.tenantId,
            name: currentUser.tenantName || 'Mi Organización',
            identificacion: '',
            tipoId: '',
            direccion: '',
            telefono: '',
            email: '',
            paginaWeb: '',
            createdAt: currentUser.createdAt ? new Date(currentUser.createdAt) : new Date(),
            isActive: true
          };
        }
      }
    });
  }

  getTipoIdLabel(tipoId?: string): string {
    if (!tipoId) return 'No especificado';
    const tipo = this.tiposId.find(t => t.value === tipoId);
    return tipo ? tipo.label : tipoId;
  }

  openEditModal(): void {
    if (this.tenant) {
      this.editForm.patchValue({
        name: this.tenant.name,
        identificacion: this.tenant.identificacion || '',
        tipoId: this.tenant.tipoId || '',
        direccion: this.tenant.direccion || '',
        telefono: this.tenant.telefono || '',
        email: this.tenant.email || '',
        paginaWeb: this.tenant.paginaWeb || ''
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

    // Confirmar cambios
    Swal.fire({
      title: '¿Actualizar Información?',
      text: 'Se actualizará la información de la organización',
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Sí, actualizar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#009ef7',
      cancelButtonColor: '#f1416c'
    }).then((result) => {
      if (result.isConfirmed) {
        this.performUpdate();
      }
    });
  }

  performUpdate(): void {
    this.isLoading = true;
    const formData = this.editForm.value;

    // Limpiar campos opcionales vacíos para evitar errores de validación
    const cleanedData = {
      name: formData.name,
      identificacion: formData.identificacion || null,
      tipoId: formData.tipoId || null,
      direccion: formData.direccion || null,
      telefono: formData.telefono || null,
      email: formData.email && formData.email.trim() !== '' ? formData.email : null,
      paginaWeb: formData.paginaWeb && formData.paginaWeb.trim() !== '' ? formData.paginaWeb : null
    };

    // Llamar al API para actualizar el tenant
    this.tenantService.updateTenant(cleanedData).subscribe({
      next: (response) => {
        // Actualizar datos locales
        this.tenant = {
          id: response.id,
          name: response.name,
          identificacion: response.identificacion,
          tipoId: response.tipoId,
          direccion: response.direccion,
          telefono: response.telefono,
          email: response.email,
          paginaWeb: response.paginaWeb,
          createdAt: response.createdAt ? new Date(response.createdAt) : undefined,
          isActive: response.isActive
        };

        // Actualizar el nombre del tenant en el storage
        const currentUser = this.storageService.getCurrentUser();
        if (currentUser) {
          const updatedUser = {
            ...currentUser,
            tenantName: response.name
          };
          this.storageService.setCurrentUser(updatedUser);
        }
        
        this.isLoading = false;
        this.closeModal();
        
        Swal.fire({
          icon: 'success',
          title: '¡Actualizado!',
          text: 'La información de la organización ha sido actualizada exitosamente',
          confirmButtonColor: '#009ef7',
          timer: 2000
        });
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Error updating tenant:', error);
        
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
