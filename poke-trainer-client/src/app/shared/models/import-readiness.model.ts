export interface ImportReadiness {
  isReady: boolean;
  message?: string | null;
  pokemonCount?: number;
  lastImportedAt?: string | null;
}
