import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { DreamTeamService } from '../dream-team/dream-team.service';
import { DreamTeamStateService } from '../dream-team/dream-team-state.service';
import { PokemonCatalogService } from '../pokemon-catalog/pokemon-catalog.service';
import { ErrorState } from '../../shared/components/error-state';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { TypeBadge } from '../../shared/components/type-badge';
import { PokemonDetails } from '../../shared/models/pokemon.model';

@Component({
  selector: 'app-pokemon-details-page',
  imports: [PokeballLoader, ErrorState, TypeBadge],
  templateUrl: './pokemon-details-page.html',
  styleUrl: './pokemon-details-page.scss'
})
export class PokemonDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalogService = inject(PokemonCatalogService);
  private readonly dreamTeamService = inject(DreamTeamService);
  private readonly dreamTeamStateService = inject(DreamTeamStateService);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly pokemon = signal<PokemonDetails | null>(null);
  readonly teamPokeApiIds = this.dreamTeamStateService.teamPokeApiIds;
  readonly fallbackImage = 'assets/pokemon-placeholder.svg';
  private readonly maxTeamSize = 5;
  ngOnInit(): void {
    this.load();
    this.loadTeamStatus();
  }

  load(): void {
    const pokeApiId = Number(this.route.snapshot.paramMap.get('id'));

    if (!pokeApiId) {
      this.errorMessage.set('Invalid Pokémon id.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.catalogService.getPokemonById(pokeApiId).subscribe({
      next: pokemon => {
        this.pokemon.set(pokemon);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.message ?? 'Details failed to load.');
      }
    });
  }

  addToTeam(): void {
  const currentPokemon = this.pokemon();

  if (!currentPokemon) {
    return;
  }

  this.errorMessage.set(null);
  this.successMessage.set(null);

  this.dreamTeamService.addPokemon({
    pokeApiId: currentPokemon.pokeApiId
  }).subscribe({
    next: () => {
      this.successMessage.set(`${currentPokemon.name} was added to your Dream Team.`);

      this.dreamTeamStateService.refresh().subscribe({
        next: () => {},
        error: () => {}
      });
    },
    error: error => {
      this.errorMessage.set(error?.message ?? 'Failed to add Pokémon to Dream Team.');
    }
  });
}

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.fallbackImage;
  }
  private loadTeamStatus(): void {
  this.dreamTeamStateService.load().subscribe({
    next: () => {},
    error: () => {
      // Details can still work without team status.
      // Backend still validates duplicates and max team size.
    }
  });
}
isInTeam(): boolean {
  const currentPokemon = this.pokemon();

  return currentPokemon
    ? this.teamPokeApiIds().has(currentPokemon.pokeApiId)
    : false;
}
isTeamFull(): boolean {
  return this.teamPokeApiIds().size >= this.maxTeamSize;
}

canAddToTeam(): boolean {
  const currentPokemon = this.pokemon();

  if (!currentPokemon) {
    return false;
  }

  return !this.isInTeam() && !this.isTeamFull();
}

getAddDisabledReason(): string | null {
  const currentPokemon = this.pokemon();

  if (!currentPokemon) {
    return null;
  }

  if (this.isInTeam()) {
    return 'This Pokémon is already in your Dream Team.';
  }

  if (this.isTeamFull()) {
    return 'Your Dream Team is full. Remove a Pokémon before adding another one.';
  }

  return null;
}
}
