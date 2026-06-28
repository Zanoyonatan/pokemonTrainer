import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { map, Observable, tap } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { TokenStorage } from './token.storage';
import {
  AuthBackendResponse,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  TrainerProfile
} from '../../shared/models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorage);

  private readonly trainerSignal = signal<TrainerProfile | null>(null);
  readonly trainer = this.trainerSignal.asReadonly();

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthBackendResponse>(`${API_BASE_URL}/auth/login`, request).pipe(
      map(response => normalizeAuthResponse(response, request.email)),
      tap(response => this.applyAuthResponse(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthBackendResponse>(`${API_BASE_URL}/auth/register`, request).pipe(
      map(response => normalizeAuthResponse(response, request.email, request.displayName)),
      tap(response => this.applyAuthResponse(response))
    );
  }

  me(): Observable<TrainerProfile> {
    return this.http.get<TrainerProfile>(`${API_BASE_URL}/auth/me`).pipe(
      tap(trainer => this.trainerSignal.set(trainer))
    );
  }

  logout(): void {
    this.tokenStorage.clearToken();
    this.trainerSignal.set(null);
  }

  hasToken(): boolean {
    return this.tokenStorage.hasToken();
  }

  private applyAuthResponse(response: AuthResponse): void {
    this.tokenStorage.setToken(response.token);
    this.trainerSignal.set(response.trainer);
  }
}

function normalizeAuthResponse(
  response: AuthBackendResponse,
  fallbackEmail: string,
  fallbackDisplayName = 'Trainer'
): AuthResponse {
  const token = response.token ?? response.jwtToken ?? response.accessToken;

  if (!token) {
    throw new Error('Login response did not contain a JWT token.');
  }

  const trainer = response.trainer ?? response.user ?? {
    email: response.email ?? fallbackEmail,
    displayName: response.displayName ?? response.trainerName ?? fallbackDisplayName,
    trainerName: response.trainerName ?? response.displayName ?? fallbackDisplayName
  };

  return {
    token,
    expiresAt: response.expiresAt,
    trainer
  };
}