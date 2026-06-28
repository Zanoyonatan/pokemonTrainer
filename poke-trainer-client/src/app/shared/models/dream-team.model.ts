import { PokemonListItem } from './pokemon.model';

export interface DreamTeamItem {
  id: number;
  pokeApiId: number;
  pokemonName: string;
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

export interface TeamAnalysisResult {
  summary: string;
  score?: number;
  strengths: string[];
  weaknesses: string[];
  recommendations?: string[];
  suggestions?: string[];
  fastestPokemon?: string;
  strongestPokemon?: string;
  bestDefensivePokemon?: string;
  averageHp?: number;
  averageAttack?: number;
  averageDefense?: number;
  averageSpeed?: number;
  recommendedPokemon?: PokemonListItem[];
}

export interface NicknameSuggestionResult {
  pokeApiId: number;
  suggestions: string[];
}
