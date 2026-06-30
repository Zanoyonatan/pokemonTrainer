import { Component, EventEmitter, Input, Output } from '@angular/core';

import { PokemonCard } from '../pokemon-card';
import { PokemonListItem } from '../../models/pokemon.model';

@Component({
  selector: 'app-pokemon-results-grid',
  imports: [PokemonCard],
  templateUrl: './pokemon-results-grid.html',
  styleUrl: './pokemon-results-grid.scss'
})
export class PokemonResultsGrid {
  private readonly maxTeamSize = 5;

  @Input() pokemons: PokemonListItem[] = [];
  @Input() teamPokeApiIds: ReadonlySet<number> = new Set<number>();

  @Output() addToTeam = new EventEmitter<PokemonListItem>();

  isInTeam(pokemon: PokemonListItem): boolean {
    return this.teamPokeApiIds.has(pokemon.pokeApiId);
  }

  isTeamFull(): boolean {
    return this.teamPokeApiIds.size >= this.maxTeamSize;
  }

  canAddToTeam(pokemon: PokemonListItem): boolean {
    return !this.isInTeam(pokemon) && !this.isTeamFull();
  }

  getAddDisabledReason(pokemon: PokemonListItem): string | null {
    if (this.isInTeam(pokemon)) {
      return 'This Pokémon is already in your Dream Team.';
    }

    if (this.isTeamFull()) {
      return 'Your Dream Team is full. Remove a Pokémon before adding another one.';
    }

    return null;
  }
}