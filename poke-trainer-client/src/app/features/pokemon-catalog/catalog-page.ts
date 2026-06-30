import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DreamTeamService } from '../dream-team/dream-team.service';
import { DreamTeamStateService } from '../dream-team/dream-team-state.service';
import { SmartSearchService } from '../smart-search/smart-search.service';
import { PokemonCatalogService } from './pokemon-catalog.service';

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
  private readonly catalogService = inject(PokemonCatalogService);
  private readonly smartSearchService = inject(SmartSearchService);
  private readonly dreamTeamService = inject(DreamTeamService);
  private readonly dreamTeamState = inject(DreamTeamStateService);

  readonly activeTab = signal<CatalogTab>('criteria');

  readonly pokemons = signal<PokemonListItem[]>([]);
  readonly smartPokemons = signal<PokemonListItem[]>([]);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalCount = signal(0);

  readonly smartSearched = signal(false);
  readonly smartExplanation = signal<string | null>(null);

  readonly teamPokeApiIds = this.dreamTeamState.teamPokeApiIds;

  readonly pageSize = 10;

  search = '';
  type = '';
  sortBy = 'name';

  smartQuery = '';

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
    this.loadCatalog();
    this.loadTeamStatus();
  }

  selectTab(tab: CatalogTab): void {
    this.activeTab.set(tab);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  loadCatalog(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.catalogService
      .getPokemon(
        this.page(),
        this.pageSize,
        this.search.trim(),
        this.type,
        this.sortBy
      )
      .subscribe({
        next: result => {
          this.pokemons.set(result.items);
          this.page.set(result.page);
          this.totalPages.set(result.totalPages);
          this.totalCount.set(result.totalCount);
          this.isLoading.set(false);
        },
        error: error => {
          this.isLoading.set(false);
          this.errorMessage.set(error?.message ?? 'Catalog failed to load.');
        }
      });
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadCatalog();
  }

  resetFilters(): void {
    this.search = '';
    this.type = '';
    this.sortBy = 'name';
    this.page.set(1);
    this.loadCatalog();
  }

  smartSearch(): void {
    const query = this.smartQuery.trim();

    if (!query) {
      this.errorMessage.set('Please enter a smart search request.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.smartExplanation.set(null);
    this.smartPokemons.set([]);
    this.smartSearched.set(true);

    this.smartSearchService.search(query, 1, this.pageSize).subscribe({
      next: response => {
        this.smartPokemons.set(response.items);
        this.smartExplanation.set(response.explanation ?? null);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.message ?? 'Smart Search failed.');
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

    this.page.update(currentPage => currentPage + 1);
    this.loadCatalog();
  }

  previousPage(): void {
    if (this.activeTab() !== 'criteria') {
      return;
    }

    if (this.page() <= 1) {
      return;
    }

    this.page.update(currentPage => currentPage - 1);
    this.loadCatalog();
  }

  addToTeam(pokemon: PokemonListItem): void {
    this.errorMessage.set(null);
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
        this.errorMessage.set(error?.message ?? 'Failed to add Pokémon to Dream Team.');
      }
    });
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