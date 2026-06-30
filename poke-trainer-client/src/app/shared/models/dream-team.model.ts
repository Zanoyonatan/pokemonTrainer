import { PokemonListItem } from './pokemon.model';

export interface DreamTeamItem {
  id: number;
  pokeApiId: number;
  name: string;
  imageUrl?: string | null;
  nickname?: string | null;
  types: string[];
  hp?: number;
  attack?: number;
  defense?: number;
  speed?: number;
}

export interface AddToDreamTeamRequest {
  pokeApiId: number;
  nickname?: string | null;
}

export interface UpdateNicknameRequest {
  nickname: string;
}

export interface TeamAnalysisPokemonHighlight {
  pokeApiId: number;
  name: string;
  nickname?: string | null;
  imageUrl?: string | null;
  value: number;
}

export interface TeamAverageStats {
  hp: number;
  attack: number;
  defense: number;
  specialAttack: number;
  specialDefense: number;
  speed: number;
  totalStats: number;
}

export interface TeamAnalysisResult {
  maxTeamSize: number;
  currentTeamSize: number;
  isFullTeam: boolean;

  teamScore?: number;

  types: string[];
  missingRecommendedTypes: string[];

  averageStats: TeamAverageStats;

  fastestPokemon?: TeamAnalysisPokemonHighlight | null;
  strongestPokemon?: TeamAnalysisPokemonHighlight | null;
  bestDefensivePokemon?: TeamAnalysisPokemonHighlight | null;

  strengths: string[];
  weaknesses: string[];
  recommendations: string[];

  summary: string;
  aiSummaryUsed: boolean;
}

export interface NicknameSuggestionResult {
  pokeApiId: number;
  suggestions: string[];
}
