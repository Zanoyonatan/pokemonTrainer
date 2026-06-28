import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { DreamTeamStateService } from '../dream-team/dream-team-state.service';
import { ErrorState } from '../../shared/components/error-state';
import { PokeballLoader } from '../../shared/components/pokeball-loader';
import { DreamTeamItem } from '../../shared/models/dream-team.model';

@Component({
  selector: 'app-dashboard-page',
  imports: [RouterLink, PokeballLoader, ErrorState],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss'
})
export class DashboardPage implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly dreamTeamState = inject(DreamTeamStateService);

  readonly trainer = this.authService.trainer;
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly team = signal<DreamTeamItem[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dreamTeamState.load().subscribe({
      next: team => {
        this.team.set(team);
        this.isLoading.set(false);
      },
      error: error => {
        this.isLoading.set(false);
        this.errorMessage.set(error?.message ?? 'Dashboard failed to load.');
      }
    });
  }
}