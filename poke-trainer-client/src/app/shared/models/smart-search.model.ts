import { PokemonListItem } from './pokemon.model';
export interface SmartSearchRequest { query: string; }
export interface SmartSearchResult { query: string; explanation: string; source: 'ai' | 'rule-based' | 'fallback'; items: PokemonListItem[]; }
