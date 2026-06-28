import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/config/api.config';
import { PokemonListItem } from '../../shared/models/pokemon.model';
import { SmartSearchRequest, SmartSearchResult } from '../../shared/models/smart-search.model';

interface SmartSearchBackendObject {
  items?: PokemonListItem[];
  results?: PokemonListItem[];
  data?: PokemonListItem[];
  query?: string;
  explanation?: string;
  source?: string;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  totalPages?: number;
}

type SmartSearchBackendResponse = SmartSearchBackendObject | PokemonListItem[];

@Injectable({
  providedIn: 'root'
})
export class SmartSearchService {
  private readonly http = inject(HttpClient);

  search(query: string, page = 1, pageSize = 5): Observable<SmartSearchResult> {
    const request: SmartSearchRequest = { query, page, pageSize };

    return this.http
      .post<SmartSearchBackendResponse>(`${API_BASE_URL}/pokemon-smart-search`, request)
      .pipe(map(response => normalizeSmartSearchResponse(query, page, pageSize, response)));
  }
}

function normalizeSmartSearchResponse(
  query: string,
  page: number,
  pageSize: number,
  response: SmartSearchBackendResponse
): SmartSearchResult {
  if (Array.isArray(response)) {
    return {
      query,
      page,
      pageSize,
      items: response,
      explanation: 'Smart Search returned matching Pokémon.',
      source: 'ai'
    };
  }

  const items = response.items ?? response.results ?? response.data ?? [];

  return {
    query: response.query ?? query,
    page: response.page ?? page,
    pageSize: response.pageSize ?? pageSize,
    totalCount: response.totalCount,
    totalPages: response.totalPages,
    items,
    explanation: response.explanation ?? 'Smart Search returned matching Pokémon.',
    source: response.source ?? 'ai'
  };
}
