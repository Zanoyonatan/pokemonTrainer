import { PokemonListItem } from './pokemon.model';

export interface SmartSearchRequest {
  query: string;
  page: number;
  pageSize: number;
}

export interface SmartSearchResult {
  query?: string;
  explanation?: string;
  source?: 'ai' | 'rule-based' | 'fallback' | string;
  items: PokemonListItem[];
  page?: number;
  pageSize?: number;
  totalCount?: number;
  totalPages?: number;
}
