import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  resetPasswordForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  passwordResetSuccess = false;
  resetToken = '';
  showPassword = false;
  showConfirmPassword = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Obtener el token de la URL
    this.route.queryParams.subscribe(params => {
      this.resetToken = params['token'] || '';
      if (!this.resetToken) {
        this.errorMessage = 'Token de restablecimiento inválido o faltante';
      }
    });

    this.initializeForm();
  }

  private initializeForm(): void {
    this.resetPasswordForm = this.fb.group({
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100),
        this.passwordComplexityValidator
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  /**
   * Validador de complejidad de contraseña
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
    const password = this.resetPasswordForm.get('password')?.value || '';
    let strength = 0;

    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^a-zA-Z0-9]/.test(password)) strength++;

    return Math.min(strength, 4);
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

  onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.resetToken) {
      this.errorMessage = 'Token de restablecimiento inválido';
      return;
    }

    if (this.resetPasswordForm.invalid) {
      this.resetPasswordForm.markAllAsTouched();
      if (this.resetPasswordForm.hasError('passwordMismatch')) {
        this.errorMessage = 'Las contraseñas no coinciden';
      } else {
        this.errorMessage = 'Por favor, completa todos los campos correctamente';
      }
      return;
    }

    this.isSubmitting = true;
    const { password } = this.resetPasswordForm.value;

    this.authService.resetPassword(this.resetToken, password).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.passwordResetSuccess = true;
        this.successMessage = response.message || 'Contraseña restablecida exitosamente';
      },
      error: (error) => {
        this.isSubmitting = false;
        if (error.error?.errors && Array.isArray(error.error.errors) && error.error.errors.length > 0) {
          this.errorMessage = error.error.errors[0].message;
        } else if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Ocurrió un error al restablecer la contraseña. Por favor, inténtalo nuevamente.';
        }
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
