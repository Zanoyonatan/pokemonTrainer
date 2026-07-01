import { Injectable, inject, signal } from '@angular/core';

import {
  PokemonCatalogRequest,
  PokemonCatalogService,
  PokemonSortDirection
} from './pokemon-catalog.service';

import { PagedResult } from '../../shared/models/paged-result.model';
import { PokemonListItem } from '../../shared/models/pokemon.model';

interface PokemonCatalogPageState {
  page: number;
  pageSize: number;
  search: string;
  type: string;
  sortBy: string;
  sortDirection: PokemonSortDirection;
}

const DEFAULT_PAGE_STATE: PokemonCatalogPageState = {
  page: 1,
  pageSize: 10,
  search: '',
  type: '',
  sortBy: '',
  sortDirection: 'asc'
};

@Injectable({
  providedIn: 'root'
})
export class PokemonCatalogStateService {
  private readonly catalogService = inject(PokemonCatalogService);

  private readonly pageStateSignal = signal<PokemonCatalogPageState>({
    ...DEFAULT_PAGE_STATE
  });

  private readonly resultSignal = signal<PagedResult<PokemonListItem> | null>(null);
  private readonly isLoadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly hasLoadedSignal = signal(false);

  readonly pageState = this.pageStateSignal.asReadonly();
  readonly result = this.resultSignal.asReadonly();
  readonly isLoading = this.isLoadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly hasLoaded = this.hasLoadedSignal.asReadonly();

  loadInitialCatalog(): void {
    if (this.hasLoadedSignal()) {
      return;
    }

    this.loadFromServer();
  }

  updateSearch(search: string): void {
    this.pageStateSignal.update(current => ({
      ...current,
      search,
      page: 1
    }));

    this.loadFromServer();
  }

  updateType(type: string): void {
    this.pageStateSignal.update(current => ({
      ...current,
      type,
      page: 1
    }));

    this.loadFromServer();
  }

  updateSort(sortBy: string, sortDirection?: PokemonSortDirection): void {
    this.pageStateSignal.update(current => ({
      ...current,
      sortBy,
      sortDirection: sortDirection ?? current.sortDirection,
      page: 1
    }));

    this.loadFromServer();
  }

  updateSortDirection(sortDirection: PokemonSortDirection): void {
    this.pageStateSignal.update(current => ({
      ...current,
      sortDirection,
      page: 1
    }));

    this.loadFromServer();
  }

  updatePage(page: number): void {
    this.pageStateSignal.update(current => ({
      ...current,
      page
    }));

    this.loadFromServer();
  }

  updatePageSize(pageSize: number): void {
    this.pageStateSignal.update(current => ({
      ...current,
      pageSize,
      page: 1
    }));

    this.loadFromServer();
  }

  refresh(): void {
    this.loadFromServer();
  }

  reset(): void {
    this.pageStateSignal.set({ ...DEFAULT_PAGE_STATE });
    this.resultSignal.set(null);
    this.errorSignal.set(null);
    this.hasLoadedSignal.set(false);

    this.loadFromServer();
  }

  private loadFromServer(): void {
    const state = this.pageStateSignal();

    const request: PokemonCatalogRequest = {
      page: state.page,
      pageSize: state.pageSize,
      search: state.search,
      type: state.type,
      sortBy: state.sortBy,
      sortDirection: state.sortDirection
    };

    this.isLoadingSignal.set(true);
    this.errorSignal.set(null);

    this.catalogService.getPokemon(request).subscribe({
      next: result => {
        this.resultSignal.set(result);
        this.hasLoadedSignal.set(true);
        this.isLoadingSignal.set(false);
      },
      error: error => {
        console.error('Failed to load pokemon catalog', error);

        this.errorSignal.set('לא הצלחנו לטעון את קטלוג הפוקימונים');
        this.isLoadingSignal.set(false);
      }
    });
  }
  applyFilters(search: string, type: string, sortBy: string): void {
  this.pageStateSignal.update(current => ({
    ...current,
    search,
    type,
    sortBy,
    page: 1
  }));

  this.loadFromServer();
}
}