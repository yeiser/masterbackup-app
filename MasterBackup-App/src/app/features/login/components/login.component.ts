import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SavedAccount } from '../../../core/models/auth.models';
import { AuthService } from '../../../core/services/auth.service';

enum LoginStep {
  SELECT_ACCOUNT = 0,
  EMAIL = 1,
  PASSWORD = 2,
  TWO_FACTOR = 3
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  LoginStep = LoginStep;
  currentStep: LoginStep = LoginStep.SELECT_ACCOUNT;
  
  emailForm!: FormGroup;
  passwordForm!: FormGroup;
  twoFactorForm!: FormGroup;
  
  savedAccounts: SavedAccount[] = [];
  selectedAccount: SavedAccount | null = null;
  
  twoFactorEnabled = false;
  userFirstName = '';
  
  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.initForms();
  }

  ngOnInit(): void {
    this.loadSavedAccounts();
  }

  private initForms(): void {
    this.emailForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.passwordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.twoFactorForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  private loadSavedAccounts(): void {
    this.savedAccounts = this.authService.getSavedAccounts();
    if (this.savedAccounts.length > 0) {
      this.currentStep = LoginStep.SELECT_ACCOUNT;
    } else {
      this.currentStep = LoginStep.EMAIL;
    }
  }

  // Paso 0: Seleccionar cuenta guardada
  selectAccount(account: SavedAccount): void {
    this.selectedAccount = account;
    this.emailForm.patchValue({
      email: account.email
    });
    this.userFirstName = account.firstName || '';
    this.currentStep = LoginStep.PASSWORD;
  }

  // Opción para usar otra cuenta
  useAnotherAccount(): void {
    this.selectedAccount = null;
    this.currentStep = LoginStep.EMAIL;
    this.emailForm.reset();
  }

  // Paso 1: Validar email
  async onEmailSubmit(): Promise<void> {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email } = this.emailForm.value;

    this.authService.validateEmail(email).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.exists) {
          this.twoFactorEnabled = response.twoFactorEnabled;
          this.userFirstName = response.firstName || '';
          this.currentStep = LoginStep.PASSWORD;
        } else {
          this.errorMessage = 'El email no está registrado en el sistema.';
        }
      },
      error: (error) => {
        this.loading = false;
        // Manejar errores de FluentValidation
        if (error.error?.errors) {
          this.errorMessage = error.error.errors.map((e: any) => e.message).join('. ');
        } else {
          this.errorMessage = error.error?.message || 'Error al validar el email. Por favor intenta nuevamente.';
        }
      }
    });
  }

  // Paso 2: Validar contraseña
  async onPasswordSubmit(): Promise<void> {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email } = this.emailForm.value;
    const { password } = this.passwordForm.value;

    this.authService.login(email, password).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.twoFactorRequired || response.requiresTwoFactor) {
          // Necesita 2FA
          this.currentStep = LoginStep.TWO_FACTOR;
        } else if (response.success) {
          // Login exitoso sin 2FA
          this.handleLoginSuccess();
        } else {
          this.errorMessage = response.message || 'Error al iniciar sesión';
        }
      },
      error: (error) => {
        this.loading = false;
        // Manejar errores de FluentValidation
        if (error.error?.errors) {
          this.errorMessage = error.error.errors.map((e: any) => e.message).join('. ');
        } else {
          this.errorMessage = error.error?.message || 'Contraseña incorrecta. Por favor intenta nuevamente.';
        }
      }
    });
  }

  // Paso 3: Verificar código 2FA
  async onTwoFactorSubmit(): Promise<void> {
    if (this.twoFactorForm.invalid) {
      this.twoFactorForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const { email } = this.emailForm.value;
    const { password } = this.passwordForm.value;
    const { code } = this.twoFactorForm.value;

    this.authService.verify2FA(email, password, code).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          this.handleLoginSuccess();
        } else {
          this.errorMessage = response.message || 'Error al verificar código 2FA';
        }
      },
      error: (error) => {
        this.loading = false;
        // Manejar errores de FluentValidation
        if (error.error?.errors) {
          this.errorMessage = error.error.errors.map((e: any) => e.message).join('. ');
        } else {
          this.errorMessage = error.error?.message || 'Código de verificación incorrecto. Por favor intenta nuevamente.';
        }
      }
    });
  }

  private handleLoginSuccess(): void {
    // Redirigir al dashboard o página principal
    this.router.navigate(['/dashboard']);
  }

  // Navegación entre pasos
  goBack(): void {
    this.errorMessage = '';
    
    if (this.currentStep === LoginStep.PASSWORD) {
      if (this.selectedAccount) {
        this.currentStep = LoginStep.SELECT_ACCOUNT;
        this.passwordForm.reset();
      } else {
        this.currentStep = LoginStep.EMAIL;
        this.passwordForm.reset();
      }
    } else if (this.currentStep === LoginStep.TWO_FACTOR) {
      this.currentStep = LoginStep.PASSWORD;
      this.twoFactorForm.reset();
    } else if (this.currentStep === LoginStep.EMAIL) {
      if (this.savedAccounts.length > 0) {
        this.currentStep = LoginStep.SELECT_ACCOUNT;
      }
    }
  }

  // Eliminar cuenta guardada
  removeSavedAccount(event: Event, account: SavedAccount): void {
    event.stopPropagation();
    this.authService.removeSavedAccount(account.email);
    this.loadSavedAccounts();
  }

  // Helpers para el template
  get canGoBack(): boolean {
    return this.currentStep !== LoginStep.SELECT_ACCOUNT && 
           !(this.currentStep === LoginStep.EMAIL && this.savedAccounts.length === 0);
  }

  getAccountInitials(account: SavedAccount): string {
    const first = account.firstName?.charAt(0) || '';
    const last = account.lastName?.charAt(0) || '';
    return (first + last).toUpperCase() || account.email.charAt(0).toUpperCase();
  }

  getAccountDisplayName(account: SavedAccount): string {
    if (account.firstName && account.lastName) {
      return `${account.firstName} ${account.lastName}`;
    }
    return account.email;
  }
}
