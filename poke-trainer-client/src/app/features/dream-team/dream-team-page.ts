import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { DreamTeamService } from './dream-team.service';
import { EmptyState } from '../../shared/components/empty-state';
import { ErrorState } from '../../shared/components/error-state';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { TypeBadge } from '../../shared/components/type-badge';
import { DreamTeamItem } from '../../shared/models/dream-team.model';

@Component({
  selector: 'app-dream-team-page',
  imports: [FormsModule, RouterLink, PokeballLoader, EmptyState, ErrorState, TypeBadge],
  templateUrl: './dream-team-page.html',
  styleUrl: './dream-team-page.scss'
})
export class DreamTeamPage implements OnInit {
  private readonly dreamTeamService = inject(DreamTeamService);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly team = signal<DreamTeamItem[]>([]);
  readonly nicknameSuggestions = signal<Record<number, string[]>>({});
  readonly generatingNicknameFor = signal<number | null>(null);

  readonly fallbackImage = 'assets/pokemon-placeholder.svg';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dreamTeamService.getTeam().subscribe({
      next: team => {
        this.team.set(team);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.message ?? 'Dream Team failed to load.');
      }
    });
  }

  remove(item: DreamTeamItem): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.dreamTeamService.removePokemon(item.id).subscribe({
      next: () => {
        this.successMessage.set(`${item.pokemonName} was removed from your Dream Team.`);
        this.load();
      },
      error: error => this.errorMessage.set(error?.message ?? 'Failed to remove Pokémon.')
    });
  }

  saveNickname(item: DreamTeamItem, nickname: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.dreamTeamService.updateNickname(item.id, nickname).subscribe({
      next: updated => {
        this.successMessage.set(`Nickname saved for ${updated.pokemonName}.`);
        this.team.update(team => team.map(current => current.id === updated.id ? updated : current));
      },
      error: error => this.errorMessage.set(error?.message ?? 'Failed to save nickname.')
    });
  }

  generateNicknames(item: DreamTeamItem): void {
    this.generatingNicknameFor.set(item.id);
    this.errorMessage.set(null);

    this.dreamTeamService.generateNicknames(item.pokeApiId, 5).subscribe({
      next: result => {
        this.generatingNicknameFor.set(null);
        this.nicknameSuggestions.update(current => ({
          ...current,
          [item.id]: result.suggestions
        }));
      },
      error: error => {
        this.generatingNicknameFor.set(null);
        this.errorMessage.set(error?.message ?? 'Failed to generate nicknames.');
      }
    });
  }

  emptySlots(): number[] {
    return Array.from({ length: Math.max(0, 5 - this.team().length) }, (_, index) => index + 1);
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = this.fallbackImage;
  }
}
