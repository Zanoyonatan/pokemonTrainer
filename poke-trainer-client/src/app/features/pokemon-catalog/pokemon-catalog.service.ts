import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/config/api.config';
import { ImportReadiness } from '../../shared/models/import-readiness.model';
import { PagedResult } from '../../shared/models/paged-result.model';
import { PokemonDetails, PokemonListItem } from '../../shared/models/pokemon.model';

interface PokemonListBackendObject {
  items?: PokemonListItem[];
  data?: PokemonListItem[];
  results?: PokemonListItem[];
  page?: number;
  pageSize?: number;
  totalCount?: number;
  totalPages?: number;
}

type PokemonListBackendResponse = PokemonListBackendObject | PokemonListItem[];

@Injectable({
  providedIn: 'root'
})
export class PokemonCatalogService {
  private readonly http = inject(HttpClient);

  getReadiness(): Observable<ImportReadiness> {
    return this.http.get<ImportReadiness>(`${API_BASE_URL}/pokemon-import-status`);
  }

  getPokemon(
    page = 1,
    pageSize = 24,
    search?: string,
    type?: string,
    sortBy?: string
  ): Observable<PagedResult<PokemonListItem>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (search) {
      params = params.set('search', search);
    }

    if (type) {
      params = params.set('type', type);
    }

    if (sortBy) {
      params = params.set('sortBy', sortBy);
    }

    return this.http
      .get<PokemonListBackendResponse>(`${API_BASE_URL}/pokemon`, { params })
      .pipe(map(response => normalizePokemonListResponse(response, page, pageSize)));
  }

  getPokemonById(pokeApiId: number): Observable<PokemonDetails> {
    return this.http.get<PokemonDetails>(`${API_BASE_URL}/pokemon/${pokeApiId}`);
  }
}

function normalizePokemonListResponse(
  response: PokemonListBackendResponse,
  page: number,
  pageSize: number
): PagedResult<PokemonListItem> {
  if (Array.isArray(response)) {
    return {
      items: response,
      page,
      pageSize,
      totalCount: response.length,
      totalPages: 1
    };
  }

  const items = response.items ?? response.data ?? response.results ?? [];

  return {
    items,
    page: response.page ?? page,
    pageSize: response.pageSize ?? pageSize,
    totalCount: response.totalCount ?? items.length,
    totalPages: response.totalPages ?? 1
  };
}
