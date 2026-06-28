import { Injectable } from '@angular/core';
const TOKEN_KEY = 'poke_trainer_ai_token';
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  getToken(): string | null { return localStorage.getItem(TOKEN_KEY); }
  setToken(token: string): void { localStorage.setItem(TOKEN_KEY, token); }
  clearToken(): void { localStorage.removeItem(TOKEN_KEY); }
  hasToken(): boolean { return !!this.getToken(); }
}
