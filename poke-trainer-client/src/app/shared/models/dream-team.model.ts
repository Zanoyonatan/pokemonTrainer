import { PokemonListItem } from './pokemon.model';
export interface DreamTeamItem { id: number; pokemonId: number; pokemonName: string; imageUrl?: string | null; nickname?: string | null; types: string[]; }
export interface AddToDreamTeamRequest { pokemonId: number; nickname?: string | null; }
export interface UpdateNicknameRequest { nickname: string; }
export interface TeamAnalysisResult { summary: string; score?: number; strengths: string[]; weaknesses: string[]; suggestions: string[]; recommendedPokemon?: PokemonListItem[]; }
export interface NicknameSuggestionResult { pokemonId: number; suggestions: string[]; }
