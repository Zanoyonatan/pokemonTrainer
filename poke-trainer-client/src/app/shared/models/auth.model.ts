export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest { trainerName: string; email: string; password: string; }
export interface AuthResponse { token: string; expiresAt?: string; trainer: TrainerProfile; }
export interface TrainerProfile { id: number; trainerName: string; email: string; }
