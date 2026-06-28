import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { PagedResult } from '../../shared/models/paged-result.model';
import { PokemonDetails, PokemonListItem } from '../../shared/models/pokemon.model';
@Injectable({ providedIn: 'root' })
export class PokemonCatalogService {
  private readonly http = inject(HttpClient);
  getPokemon(page = 1, pageSize = 24, search?: string, type?: string): Observable<PagedResult<PokemonListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (type) params = params.set('type', type);
    return this.http.get<PagedResult<PokemonListItem>>(`${API_BASE_URL}/pokemon`, { params });
  }
  getPokemonById(id: number): Observable<PokemonDetails> { return this.http.get<PokemonDetails>(`${API_BASE_URL}/pokemon/${id}`); }
}
