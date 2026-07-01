import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/config/api.config';
import { ImportReadiness } from '../../shared/models/import-readiness.model';
import { PagedResult } from '../../shared/models/paged-result.model';
import { PokemonDetails, PokemonListItem } from '../../shared/models/pokemon.model';

export type PokemonSortDirection = 'asc' | 'desc';

export interface PokemonCatalogRequest {
  page?: number;
  pageSize?: number;
  search?: string;
  type?: string;
  sortBy?: string;
  sortDirection?: PokemonSortDirection;
}

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
    requestOrPage: PokemonCatalogRequest | number = 1,
    pageSize = 10,
    search?: string,
    type?: string,
    sortBy?: string
  ): Observable<PagedResult<PokemonListItem>> {
    const request: PokemonCatalogRequest =
      typeof requestOrPage === 'object'
        ? requestOrPage
        : {
            page: requestOrPage,
            pageSize,
            search,
            type,
            sortBy
          };

    const resolvedPage = request.page ?? 1;
    const resolvedPageSize = request.pageSize ?? 10;

    let params = new HttpParams()
      .set('page', resolvedPage)
      .set('pageSize', resolvedPageSize);

    const trimmedSearch = request.search?.trim();
    const selectedType = request.type?.trim();
    const selectedSortBy = request.sortBy?.trim();

    if (trimmedSearch) {
      params = params.set('search', trimmedSearch);
    }

    if (selectedType) {
      params = params.set('type', selectedType);
    }

    if (selectedSortBy) {
      params = params.set('sortBy', selectedSortBy);
    }

    if (request.sortDirection) {
      params = params.set('sortDirection', request.sortDirection);
    }

    return this.http
      .get<PokemonListBackendResponse>(`${API_BASE_URL}/pokemon`, { params })
      .pipe(
        map(response =>
          normalizePokemonListResponse(response, resolvedPage, resolvedPageSize)
        )
      );
  }

  getPokemonById(pokeApiId: number): Observable<PokemonDetails> {
    return this.http
      .get<PokemonDetails>(`${API_BASE_URL}/pokemon/${pokeApiId}`)
      .pipe(map(response => normalizePokemonDetailsResponse(response)));
  }
}

function getStat(pokemon: PokemonDetails, statName: string): number | undefined {
  return pokemon.stats?.find(stat => stat.name === statName)?.baseStat;
}

function normalizePokemonDetailsResponse(response: PokemonDetails): PokemonDetails {
  return {
    ...response,
    hp: response.hp ?? getStat(response, 'hp'),
    attack: response.attack ?? getStat(response, 'attack'),
    defense: response.defense ?? getStat(response, 'defense'),
    speed: response.speed ?? getStat(response, 'speed')
  };
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
