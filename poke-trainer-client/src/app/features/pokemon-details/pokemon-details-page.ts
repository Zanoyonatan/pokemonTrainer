import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { DreamTeamService } from '../dream-team/dream-team.service';
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

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly pokemon = signal<PokemonDetails | null>(null);

  readonly fallbackImage = 'assets/pokemon-placeholder.svg';

  ngOnInit(): void {
    this.load();
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
    const pokemon = this.pokemon();

    if (!pokemon) {
      return;
    }

    this.successMessage.set(null);
    this.errorMessage.set(null);

    this.dreamTeamService.addPokemon({ pokeApiId: pokemon.pokeApiId }).subscribe({
      next: () => this.successMessage.set(`${pokemon.name} was added to your Dream Team.`),
      error: error => this.errorMessage.set(error?.message ?? 'Failed to add Pokémon to Dream Team.')
    });
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.fallbackImage;
  }
}
