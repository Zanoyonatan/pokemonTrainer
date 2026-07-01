import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DreamTeamService } from '../dream-team/dream-team.service';
import { DreamTeamStateService } from '../dream-team/dream-team-state.service';
import { SmartSearchService } from '../smart-search/smart-search.service';
import { getUserFriendlyErrorMessage } from '../../core/errors/user-friendly-error-message';
import { PokemonCatalogStateService } from './pokemon-catalog-state.service';

import { EmptyState } from '../../shared/components/empty-state';
import { ErrorState } from '../../shared/components/error-state';
import { PokemonResultsGrid } from '../../shared/components/pokemon-results-grid/pokemon-results-grid';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { PokemonListItem } from '../../shared/models/pokemon.model';

type CatalogTab = 'criteria' | 'smart';

@Component({
  selector: 'app-catalog-page',
  imports: [
    FormsModule,
    PokemonResultsGrid,
    PokeballLoader,
    EmptyState,
    ErrorState
  ],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.scss'
})
export class CatalogPage implements OnInit {
  private readonly catalogState = inject(PokemonCatalogStateService);
  private readonly smartSearchService = inject(SmartSearchService);
  private readonly dreamTeamService = inject(DreamTeamService);
  private readonly dreamTeamState = inject(DreamTeamStateService);

  readonly activeTab = signal<CatalogTab>('criteria');

  readonly smartPokemons = signal<PokemonListItem[]>([]);
  readonly smartIsLoading = signal(false);
  readonly localErrorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly smartSearched = signal(false);
  readonly smartExplanation = signal<string | null>(null);

  readonly teamPokeApiIds = this.dreamTeamState.teamPokeApiIds;

  readonly pageSize = 20;

  search = '';
  type = '';
  sortBy = 'name';

  smartQuery = '';

  readonly pageState = this.catalogState.pageState;

  readonly pokemons = computed(() => this.catalogState.result()?.items ?? []);
  readonly page = computed(() => this.catalogState.pageState().page);
  readonly totalPages = computed(() => this.catalogState.result()?.totalPages ?? 1);
  readonly totalCount = computed(() => this.catalogState.result()?.totalCount ?? 0);

  readonly isLoading = computed(() =>
    this.activeTab() === 'criteria'
      ? this.catalogState.isLoading()
      : this.smartIsLoading()
  );

  readonly errorMessage = computed(() => {
    const localError = this.localErrorMessage();

    if (localError) {
      return localError;
    }

    if (this.activeTab() === 'criteria') {
      return this.catalogState.error();
    }

    return null;
  });

  readonly smartSearchExamples = [
    'find best 3 pokemon',
    'best fire pokemon with high hp',
    'fastest electric pokemon',
    'strong water pokemon',
    'defensive grass pokemon',
    'legendary pokemon'
  ];

  readonly types = [
    'normal',
    'fire',
    'water',
    'electric',
    'grass',
    'ice',
    'fighting',
    'poison',
    'ground',
    'flying',
    'psychic',
    'bug',
    'rock',
    'ghost',
    'dragon',
    'dark',
    'steel',
    'fairy'
  ];

  readonly currentPokemons = computed(() =>
    this.activeTab() === 'criteria'
      ? this.pokemons()
      : this.smartPokemons()
  );

  readonly currentTotalCount = computed(() =>
    this.activeTab() === 'criteria'
      ? this.totalCount()
      : this.smartPokemons().length
  );

  readonly currentPage = computed(() =>
    this.activeTab() === 'criteria'
      ? this.page()
      : 1
  );

  readonly currentTotalPages = computed(() =>
    this.activeTab() === 'criteria'
      ? this.totalPages()
      : 1
  );

  readonly shouldShowEmptyState = computed(() => {
    if (this.isLoading() || this.errorMessage()) {
      return false;
    }

    if (this.activeTab() === 'criteria') {
      return this.pokemons().length === 0;
    }

    return this.smartSearched() && this.smartPokemons().length === 0;
  });

  ngOnInit(): void {
    this.syncFiltersFromCatalogState();
    this.catalogState.loadInitialCatalog();
    this.loadTeamStatus();
  }

  selectTab(tab: CatalogTab): void {
    this.activeTab.set(tab);
    this.localErrorMessage.set(null);
    this.successMessage.set(null);
  }

  loadCatalog(): void {
    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.catalogState.refresh();
  }

  applyFilters(): void {
    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.catalogState.applyFilters(
      this.search.trim(),
      this.type,
      this.sortBy
    );
  }

  resetFilters(): void {
    this.search = '';
    this.type = '';
    this.sortBy = 'name';

    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.catalogState.reset();
  }

  smartSearch(): void {
    const query = this.smartQuery.trim();

    if (!query) {
      this.localErrorMessage.set('Please enter a smart search request.');
      return;
    }

    this.smartIsLoading.set(true);
    this.localErrorMessage.set(null);
    this.successMessage.set(null);
    this.smartExplanation.set(null);
    this.smartPokemons.set([]);
    this.smartSearched.set(true);

    this.smartSearchService.search(query, 1, this.pageSize).subscribe({
      next: response => {
        this.smartPokemons.set(response.items);
        this.smartExplanation.set(response.explanation ?? null);
        this.smartIsLoading.set(false);
      },
      error: error => {
        this.smartIsLoading.set(false);
        this.localErrorMessage.set(
          getUserFriendlyErrorMessage(
            error,
            'Smart Search is temporarily unavailable. Please try a simpler search or try again later.'
          )
        );
      }
    });
  }

  useSmartExample(example: string): void {
    this.smartQuery = example;
    this.smartSearch();
  }

  nextPage(): void {
    if (this.activeTab() !== 'criteria') {
      return;
    }

    if (this.page() >= this.totalPages()) {
      return;
    }

    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.catalogState.updatePage(this.page() + 1);
  }

  previousPage(): void {
    if (this.activeTab() !== 'criteria') {
      return;
    }

    if (this.page() <= 1) {
      return;
    }

    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.catalogState.updatePage(this.page() - 1);
  }

  addToTeam(pokemon: PokemonListItem): void {
    this.localErrorMessage.set(null);
    this.successMessage.set(null);

    this.dreamTeamService.addPokemon({ pokeApiId: pokemon.pokeApiId }).subscribe({
      next: () => {
        this.successMessage.set(`${pokemon.name} was added to your Dream Team.`);

        this.dreamTeamState.refresh().subscribe({
          next: () => {},
          error: () => {}
        });
      },
      error: error => {
        this.localErrorMessage.set(
          getUserFriendlyErrorMessage(
            error,
            'We could not add this Pokémon to your Dream Team.'
          )
        );
      }
    });
  }

  private syncFiltersFromCatalogState(): void {
    const state = this.catalogState.pageState();

    this.search = state.search;
    this.type = state.type;
    this.sortBy = state.sortBy || 'name';
  }

  private loadTeamStatus(): void {
    this.dreamTeamState.load().subscribe({
      error: () => {
        // Catalog can still work without team status.
        // Backend still validates duplicates and max team size.
      }
    });
  }
}