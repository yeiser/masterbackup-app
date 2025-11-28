import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TextFormatHelper } from '../../../shared/helpers/text-format.helper';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  forgotPasswordForm!: FormGroup;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  emailSent = false;
  sentToEmail = '';
  private unsubscribeEmailLowerCase?: () => void;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.setupEmailLowerCase();
  }

  private initializeForm(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  private setupEmailLowerCase(): void {
    this.unsubscribeEmailLowerCase = TextFormatHelper.setupAutoLowerCase(
      this.forgotPasswordForm,
      'email'
    );
  }

  onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.emailSent = false;

    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      this.errorMessage = 'Por favor, ingresa un correo electrónico válido';
      return;
    }

    this.isSubmitting = true;
    const { email } = this.forgotPasswordForm.value;

    this.authService.forgotPassword(email).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          // Email existe y se envió el enlace
          this.emailSent = true;
          this.sentToEmail = email;
          this.forgotPasswordForm.reset();
        } else {
          this.errorMessage = response.message || 'Error al procesar la solicitud';
        }
      },
      error: (error) => {
        this.isSubmitting = false;
        // Manejar el caso cuando el email no existe
        if (error.status === 404 || error.error?.message?.includes('not found') || error.error?.message?.includes('no encontrado')) {
          this.errorMessage = 'No existe una cuenta registrada con este correo electrónico';
        } else if (error.error?.errors && Array.isArray(error.error.errors) && error.error.errors.length > 0) {
          const firstError = error.error.errors[0].message;
          if (firstError.toLowerCase().includes('not found') || firstError.toLowerCase().includes('no encontrado')) {
            this.errorMessage = 'No existe una cuenta registrada con este correo electrónico';
          } else {
            this.errorMessage = firstError;
          }
        } else if (error.error?.message) {
          this.errorMessage = error.error.message;
        } else {
          this.errorMessage = 'Ocurrió un error al procesar la solicitud. Por favor, inténtalo nuevamente.';
        }
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    if (this.unsubscribeEmailLowerCase) {
      this.unsubscribeEmailLowerCase();
    }
  }
}
