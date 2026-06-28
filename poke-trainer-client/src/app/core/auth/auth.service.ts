import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';
import { TokenStorage } from './token.storage';
import { AuthResponse, LoginRequest, RegisterRequest, TrainerProfile } from '../../shared/models/auth.model';
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorage);
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/login`, request).pipe(tap(r => this.tokenStorage.setToken(r.token)));
  }
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_BASE_URL}/auth/register`, request).pipe(tap(r => this.tokenStorage.setToken(r.token)));
  }
  me(): Observable<TrainerProfile> { return this.http.get<TrainerProfile>(`${API_BASE_URL}/auth/me`); }
  logout(): void { this.tokenStorage.clearToken(); }
  isAuthenticated(): boolean { return this.tokenStorage.hasToken(); }
}
