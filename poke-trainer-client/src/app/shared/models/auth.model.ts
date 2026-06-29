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
  id?: string | number;
  userId?: string;
  displayName?: string;
  trainerName?: string;
  name?: string;
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

  id?: string | number;
  userId?: string;
  email?: string;
  displayName?: string;
  trainerName?: string;
  name?: string;
}