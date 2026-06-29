import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DreamTeamService } from '../dream-team/dream-team.service';
import { DreamTeamStateService } from '../dream-team/dream-team-state.service';
import { PokemonCatalogService } from './pokemon-catalog.service';
import { EmptyState } from '../../shared/components/empty-state';
import { ErrorState } from '../../shared/components/error-state';
import { PokemonCard } from '../../shared/components/pokemon-card';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { PokemonListItem } from '../../shared/models/pokemon.model';

@Component({
  selector: 'app-catalog-page',
  imports: [FormsModule, PokemonCard, PokeballLoader, EmptyState, ErrorState],
  templateUrl: './catalog-page.html',
  styleUrl: './catalog-page.scss'
})
export class CatalogPage implements OnInit {
  private readonly catalogService = inject(PokemonCatalogService);
  private readonly dreamTeamService = inject(DreamTeamService);
  private readonly dreamTeamState = inject(DreamTeamStateService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly pokemons = signal<PokemonListItem[]>([]);

  readonly teamPokeApiIds = this.dreamTeamState.teamPokeApiIds;

  page = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;

  search = '';
  type = '';
  sortBy = 'name';

  readonly types = [
    'normal', 'fire', 'water', 'electric', 'grass', 'ice', 'fighting',
    'poison', 'ground', 'flying', 'psychic', 'bug', 'rock', 'ghost',
    'dragon', 'dark', 'steel', 'fairy'
  ];

  ngOnInit(): void {
    this.loadCatalog();
    this.loadTeamStatus();
  }

  loadCatalog(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.catalogService.getPokemon(this.page, this.pageSize, this.search, this.type, this.sortBy).subscribe({
      next: result => {
        this.pokemons.set(result.items);
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalPages = result.totalPages;
        this.totalCount = result.totalCount;
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.message ?? 'Catalog failed to load.');
      }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.loadCatalog();
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadCatalog();
    }
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadCatalog();
    }
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

  isInTeam(pokemon: PokemonListItem): boolean {
    return this.teamPokeApiIds().has(pokemon.pokeApiId);
  }

  private loadTeamStatus(): void {
    this.dreamTeamState.load().subscribe({
      next: () => {},
      error: () => {
        // Catalog can still work without team status.
        // The backend still validates duplicates and max team size.
      }
    });
  }
}
