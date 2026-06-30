import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/config/api.config';
import { PokemonListItem } from '../../shared/models/pokemon.model';
import { SmartSearchRequest, SmartSearchResult } from '../../shared/models/smart-search.model';

interface SmartSearchBackendPagedResult {
  items?: PokemonListItem[];
  data?: PokemonListItem[];
  results?: PokemonListItem[];
  page?: number;
  pageSize?: number;
  totalCount?: number;
  totalPages?: number;
}

interface SmartSearchBackendCriteria {
  originalQuery?: string;
  detectedIntents?: string[];
  sortBy?: string;
  sortDirection?: string;
  requestedCount?: number;
}

interface SmartSearchBackendObject {
  items?: PokemonListItem[];
  data?: PokemonListItem[];

  // Supports both:
  // results: PokemonListItem[]
  // results: { items: PokemonListItem[], totalCount, totalPages }
  results?: PokemonListItem[] | SmartSearchBackendPagedResult;

  criteria?: SmartSearchBackendCriteria;

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
      .pipe(
        map(response =>
          normalizeSmartSearchResponse(query, page, pageSize, response)
        )
      );
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
      totalCount: response.length,
      totalPages: 1,
      items: response,
      explanation: 'Smart Search returned matching Pokémon.',
      source: 'ai'
    };
  }

  const nestedResults = response.results;

  if (Array.isArray(nestedResults)) {
    return {
      query: response.query ?? response.criteria?.originalQuery ?? query,
      page: response.page ?? page,
      pageSize: response.pageSize ?? pageSize,
      totalCount: response.totalCount ?? nestedResults.length,
      totalPages: response.totalPages ?? 1,
      items: nestedResults,
      explanation: response.explanation ?? 'Smart Search returned matching Pokémon.',
      source: response.source ?? 'ai'
    };
  }

  if (nestedResults) {
    const items =
      nestedResults.items ??
      nestedResults.data ??
      nestedResults.results ??
      [];

    return {
      query: response.query ?? response.criteria?.originalQuery ?? query,
      page: nestedResults.page ?? response.page ?? page,
      pageSize: nestedResults.pageSize ?? response.pageSize ?? pageSize,
      totalCount:
        nestedResults.totalCount ??
        response.totalCount ??
        items.length,
      totalPages:
        nestedResults.totalPages ??
        response.totalPages ??
        1,
      items,
      explanation: response.explanation ?? 'Smart Search returned matching Pokémon.',
      source: response.source ?? 'ai'
    };
  }

  const items = response.items ?? response.data ?? [];

  return {
    query: response.query ?? response.criteria?.originalQuery ?? query,
    page: response.page ?? page,
    pageSize: response.pageSize ?? pageSize,
    totalCount: response.totalCount ?? items.length,
    totalPages: response.totalPages ?? 1,
    items,
    explanation: response.explanation ?? 'Smart Search returned matching Pokémon.',
    source: response.source ?? 'ai'
  };
}