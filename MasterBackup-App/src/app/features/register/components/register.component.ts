import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterDto } from '../../../core/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  isSubmitting: boolean = false;
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;
  fieldErrors: Map<string, string> = new Map();

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  private initializeForm(): void {
    this.registerForm = this.fb.group({
      email: ['', [
        Validators.required,
        Validators.email,
        Validators.maxLength(255)
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100),
        this.passwordComplexityValidator
      ]],
      confirmPassword: ['', [Validators.required]],
      firstName: ['', [
        Validators.required,
        Validators.maxLength(50)
      ]],
      lastName: ['', [
        Validators.required,
        Validators.maxLength(50)
      ]],
      tenantName: ['', [
        Validators.required,
        Validators.maxLength(100)
      ]],
      enableTwoFactor: [false]
    }, { validators: this.passwordMatchValidator });
  }

  /**
   * Validador de complejidad de contraseña
   * Debe tener al menos: 1 mayúscula, 1 minúscula, 1 dígito
   */
  private passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) {
      return null;
    }

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasDigit = /[0-9]/.test(value);

    const valid = hasUpperCase && hasLowerCase && hasDigit;

    if (!valid) {
      return { 
        passwordComplexity: 'La contraseña debe contener al menos una mayúscula, una minúscula y un dígito' 
      };
    }

    return null;
  }

  /**
   * Validador que verifica que password y confirmPassword coincidan
   */
  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;

    if (password !== confirmPassword) {
      return { passwordMismatch: true };
    }

    return null;
  }

  /**
   * Calcula la fuerza de la contraseña
   */
  getPasswordStrength(): number {
    const password = this.registerForm.get('password')?.value || '';
    let strength = 0;

    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    return Math.min(strength, 4); // Máximo 4 niveles
  }

  /**
   * Obtiene el color de la barra de fuerza
   */
  getPasswordStrengthColor(): string {
    const strength = this.getPasswordStrength();
    const colors = ['', 'bg-danger', 'bg-warning', 'bg-info', 'bg-success'];
    return colors[strength];
  }

  /**
   * Obtiene el texto de la fuerza de contraseña
   */
  getPasswordStrengthText(): string {
    const strength = this.getPasswordStrength();
    const texts = ['', 'Débil', 'Regular', 'Buena', 'Fuerte'];
    return texts[strength];
  }

  /**
   * Toggle para mostrar/ocultar contraseña
   */
  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  /**
   * Toggle para mostrar/ocultar confirmación de contraseña
   */
  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  /**
   * Obtener mensaje de error para un campo específico
   */
  getFieldError(fieldName: string): string {
    return this.fieldErrors.get(fieldName) || '';
  }

  /**
   * Verificar si un campo tiene errores (de validación local o del backend)
   */
  hasFieldError(fieldName: string): boolean {
    const control = this.registerForm.get(fieldName);
    return (control?.invalid && (control?.dirty || control?.touched)) || 
           this.fieldErrors.has(fieldName);
  }

  /**
   * Obtener el primer error del formulario
   */
  private getFirstFormError(): string {
    const fieldNames = ['tenantName', 'firstName', 'lastName', 'email', 'password', 'confirmPassword'];
    const fieldLabels: { [key: string]: string } = {
      tenantName: 'Nombre de la Organización',
      firstName: 'Nombre',
      lastName: 'Apellido',
      email: 'Correo Electrónico',
      password: 'Contraseña',
      confirmPassword: 'Confirmar Contraseña'
    };

    // Verificar errores de formulario (como passwordMismatch)
    if (this.registerForm.hasError('passwordMismatch')) {
      return 'Las contraseñas no coinciden';
    }

    // Buscar el primer campo con error
    for (const fieldName of fieldNames) {
      const control = this.registerForm.get(fieldName);
      if (control && control.invalid) {
        const label = fieldLabels[fieldName];
        const errors = control.errors;

        if (errors?.['required']) {
          return `${label} es requerido`;
        }
        if (errors?.['email']) {
          return `${label} debe ser un correo electrónico válido`;
        }
        if (errors?.['minlength']) {
          const minLength = errors['minlength'].requiredLength;
          return `${label} debe tener al menos ${minLength} caracteres`;
        }
        if (errors?.['maxlength']) {
          const maxLength = errors['maxlength'].requiredLength;
          return `${label} no puede exceder ${maxLength} caracteres`;
        }
        if (errors?.['passwordComplexity']) {
          return errors['passwordComplexity'];
        }
      }
    }

    return 'Por favor, corrija los errores en el formulario';
  }

  /**
   * Maneja el submit del formulario
   */
  onSubmit(): void {
    // Limpiar mensajes previos
    this.errorMessage = '';
    this.successMessage = '';
    this.fieldErrors.clear();

    // Validar formulario
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.errorMessage = this.getFirstFormError();
      return;
    }

    this.isSubmitting = true;

    // Preparar DTO (sin confirmPassword)
    const { confirmPassword, ...registerData } = this.registerForm.value;
    const dto: RegisterDto = registerData;

    this.authService.register(dto).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = '¡Registro exitoso! Redirigiendo...';
          
          // Redirigir después de 1.5 segundos
          setTimeout(() => {
            this.router.navigate(['/dashboard']);
          }, 1500);
        }
      },
      error: (error) => {
        this.isSubmitting = false;
        
        // Manejar errores de FluentValidation - mostrar solo el primero
        if (error.error?.errors && Array.isArray(error.error.errors) && error.error.errors.length > 0) {
          const firstError = error.error.errors[0];
          this.errorMessage = firstError.message;
        } else if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Ocurrió un error al procesar el registro. Por favor, intente nuevamente.';
        }
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Navegar a login
   */
  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
