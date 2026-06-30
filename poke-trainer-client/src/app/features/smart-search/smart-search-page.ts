import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DreamTeamService } from '../dream-team/dream-team.service';
import { getUserFriendlyErrorMessage } from '../../core/errors/user-friendly-error-message';
import { SmartSearchService } from './smart-search.service';
import { ErrorState } from '../../shared/components/error-state';
import { PokemonCard } from '../../shared/components/pokemon-card';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { PokemonListItem } from '../../shared/models/pokemon.model';
import { SmartSearchResult } from '../../shared/models/smart-search.model';

@Component({
  selector: 'app-smart-search-page',
  imports: [FormsModule, PokemonCard, PokeballLoader, ErrorState],
  templateUrl: './smart-search-page.html',
  styleUrl: './smart-search-page.scss'
})
export class SmartSearchPage {
  private readonly smartSearchService = inject(SmartSearchService);
  private readonly dreamTeamService = inject(DreamTeamService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly result = signal<SmartSearchResult | null>(null);
  readonly teamPokeApiIds = signal<Set<number>>(new Set<number>());

  query = '';
  page = 1;
  pageSize = 5;

  readonly prompts = [
    'find me top 10 small electric pokemon',
    'find me fast fire pokemon',
    'find me defensive water pokemon',
    'find me strong pokemon with high attack'
  ];

  constructor() {
    this.loadTeamIds();
  }

  usePrompt(prompt: string): void {
    this.query = prompt;
  }

  search(): void {
    const query = this.query.trim();

    if (!query) {
      this.errorMessage.set('Please enter a smart search query.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.result.set(null);

    this.smartSearchService.search(query, this.page, this.pageSize).subscribe({
      next: result => {
        this.result.set(result);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(getUserFriendlyErrorMessage(error, 'Smart Search is temporarily unavailable. Please try a simpler search or try again later.'));
      }
    });
  }

  addToTeam(pokemon: PokemonListItem): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.dreamTeamService.addPokemon({ pokeApiId: pokemon.pokeApiId }).subscribe({
      next: () => {
        this.successMessage.set(`${pokemon.name} was added to your Dream Team.`);
        this.loadTeamIds();
      },
      error: error => this.errorMessage.set(getUserFriendlyErrorMessage(error, 'We could not add this Pokémon to your Dream Team.'))
    });
  }

  isInTeam(pokemon: PokemonListItem): boolean {
    return this.teamPokeApiIds().has(pokemon.pokeApiId);
  }

  private loadTeamIds(): void {
    this.dreamTeamService.getTeam().subscribe({
      next: team => this.teamPokeApiIds.set(new Set(team.map(item => item.pokeApiId))),
      error: () => this.teamPokeApiIds.set(new Set<number>())
    });
  }
}
