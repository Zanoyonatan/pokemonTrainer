export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface TrainerProfile {
  id?: number;
  displayName?: string;
  trainerName?: string;
  email: string;
}

export interface AuthResponse {
  token: string;
  expiresAt?: string;
  trainer: TrainerProfile;
}

export interface AuthBackendResponse {
  token?: string;
  jwtToken?: string;
  accessToken?: string;
  expiresAt?: string;

  trainer?: TrainerProfile;
  user?: TrainerProfile;

  email?: string;
  displayName?: string;
  trainerName?: string;
}