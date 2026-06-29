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
      map(response => normalizeLoginResponse(response, request.email)),
      tap(response => this.applyAuthResponse(response))
    );
  }

  register(request: RegisterRequest): Observable<TrainerProfile> {
    return this.http.post<TrainerProfile>(`${API_BASE_URL}/auth/register`, request);
  }

  me(): Observable<TrainerProfile> {
    return this.http.get<TrainerProfile>(`${API_BASE_URL}/auth/me`).pipe(
      map(profile => {
        const currentTrainer = this.trainerSignal();

        const displayName =
          profile.displayName ??
          profile.trainerName ??
          profile.name ??
          currentTrainer?.displayName ??
          currentTrainer?.trainerName ??
          currentTrainer?.name;

        return {
          ...currentTrainer,
          ...profile,
          id: profile.id ?? profile.userId ?? currentTrainer?.id,
          userId: profile.userId ?? currentTrainer?.userId,
          displayName,
          trainerName: profile.trainerName ?? displayName,
          name: profile.name ?? displayName,
          email: profile.email ?? currentTrainer?.email ?? ''
        };
      }),
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

function normalizeLoginResponse(
  response: AuthBackendResponse,
  fallbackEmail: string,
  fallbackDisplayName = 'Trainer'
): AuthResponse {
  const token = response.token ?? response.jwtToken ?? response.accessToken;

  if (!token) {
    throw new Error('Login response did not contain a JWT token.');
  }

  const backendTrainer = response.trainer ?? response.user;

  const displayName =
    backendTrainer?.displayName ??
    backendTrainer?.trainerName ??
    backendTrainer?.name ??
    response.displayName ??
    response.trainerName ??
    response.name ??
    fallbackDisplayName;

  const trainer: TrainerProfile = backendTrainer ?? {
    email: response.email ?? fallbackEmail,
    displayName,
    trainerName: displayName,
    name: displayName
  };

  return {
    token,
    expiresAt: response.expiresAt,
    trainer: {
      ...trainer,
      displayName: trainer.displayName ?? trainer.trainerName ?? trainer.name ?? displayName,
      trainerName: trainer.trainerName ?? trainer.displayName ?? trainer.name ?? displayName,
      name: trainer.name ?? trainer.displayName ?? trainer.trainerName ?? displayName,
      email: trainer.email ?? fallbackEmail
    }
  };
}