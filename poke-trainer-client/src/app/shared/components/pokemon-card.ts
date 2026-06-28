import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PokemonListItem } from '../models/pokemon.model';
import { TypeBadge } from './type-badge';

@Component({
  selector: 'app-pokemon-card',
  imports: [RouterLink, TypeBadge],
  templateUrl: './pokemon-card.html',
  styleUrl: './pokemon-card.scss'
})
export class PokemonCard {
  @Input({ required: true }) pokemon!: PokemonListItem;
  @Input() isInTeam = false;

  @Output() addToTeam = new EventEmitter<PokemonListItem>();

  readonly fallbackImage = 'assets/pokemon-placeholder.svg';

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.fallbackImage;
  }
}
