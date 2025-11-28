// Modelos de autenticación

export interface LoginDto {
  email: string;
  password: string;
  twoFactorCode?: string;
}

export interface ValidateEmailDto {
  email: string;
}

export interface Verify2FADto {
  email: string;
  password: string;
  twoFactorCode: string;
}

export interface AuthResponse {
  success: boolean;
  message?: string;
  token?: string;
  apiKey?: string;
  userId?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  role?: string;
  tenantId?: string;
  twoFactorRequired?: boolean;
  requiresTwoFactor?: boolean;
  user?: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
  };
}

export interface EmailValidationResponse {
  exists: boolean;
  twoFactorEnabled: boolean;
  firstName?: string;
  lastName?: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  tenantName: string;
  enableTwoFactor: boolean;
}

export interface SavedAccount {
  email: string;
  firstName?: string;
  lastName?: string;
  lastLogin: Date;
  avatar?: string;
}
