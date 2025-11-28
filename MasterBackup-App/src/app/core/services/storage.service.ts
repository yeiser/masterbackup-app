import { Injectable } from '@angular/core';
import { SavedAccount } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private readonly SAVED_ACCOUNTS_KEY = 'masterbackup_saved_accounts';
  private readonly CURRENT_USER_KEY = 'masterbackup_current_user';
  private readonly AUTH_TOKEN_KEY = 'masterbackup_auth_token';
  private readonly API_KEY = 'masterbackup_api_key';

  constructor() {}

  // Gestión de cuentas guardadas
  getSavedAccounts(): SavedAccount[] {
    const accounts = localStorage.getItem(this.SAVED_ACCOUNTS_KEY);
    if (!accounts) return [];
    
    try {
      return JSON.parse(accounts).map((acc: any) => ({
        ...acc,
        lastLogin: new Date(acc.lastLogin)
      }));
    } catch {
      return [];
    }
  }

  saveAccount(account: SavedAccount): void {
    const accounts = this.getSavedAccounts();
    
    // Buscar si ya existe la cuenta (solo por email)
    const existingIndex = accounts.findIndex(
      acc => acc.email === account.email
    );

    if (existingIndex >= 0) {
      // Actualizar cuenta existente
      accounts[existingIndex] = { ...account, lastLogin: new Date() };
    } else {
      // Agregar nueva cuenta
      accounts.push({ ...account, lastLogin: new Date() });
    }

    // Ordenar por último login (más reciente primero)
    accounts.sort((a, b) => b.lastLogin.getTime() - a.lastLogin.getTime());

    localStorage.setItem(this.SAVED_ACCOUNTS_KEY, JSON.stringify(accounts));
  }

  removeAccount(email: string): void {
    const accounts = this.getSavedAccounts();
    const filtered = accounts.filter(acc => acc.email !== email);
    localStorage.setItem(this.SAVED_ACCOUNTS_KEY, JSON.stringify(filtered));
  }

  clearSavedAccounts(): void {
    localStorage.removeItem(this.SAVED_ACCOUNTS_KEY);
  }

  // Gestión de token de autenticación
  setAuthToken(token: string): void {
    localStorage.setItem(this.AUTH_TOKEN_KEY, token);
  }

  getAuthToken(): string | null {
    return localStorage.getItem(this.AUTH_TOKEN_KEY);
  }

  removeAuthToken(): void {
    localStorage.removeItem(this.AUTH_TOKEN_KEY);
  }

  // Gestión de API Key
  setApiKey(apiKey: string): void {
    localStorage.setItem(this.API_KEY, apiKey);
  }

  getApiKey(): string | null {
    return localStorage.getItem(this.API_KEY);
  }

  removeApiKey(): void {
    localStorage.removeItem(this.API_KEY);
  }

  // Gestión de usuario actual
  setCurrentUser(user: any): void {
    localStorage.setItem(this.CURRENT_USER_KEY, JSON.stringify(user));
  }

  getCurrentUser(): any {
    const user = localStorage.getItem(this.CURRENT_USER_KEY);
    return user ? JSON.parse(user) : null;
  }

  removeCurrentUser(): void {
    localStorage.removeItem(this.CURRENT_USER_KEY);
  }

  // Limpiar toda la sesión
  clearSession(): void {
    this.removeAuthToken();
    this.removeApiKey();
    this.removeCurrentUser();
  }
}
