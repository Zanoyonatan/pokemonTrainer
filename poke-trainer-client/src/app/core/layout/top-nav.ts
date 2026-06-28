import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { DreamTeamStateService } from '../../features/dream-team/dream-team-state.service';

@Component({
  selector: 'app-top-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './top-nav.html',
  styleUrl: './top-nav.scss'
})
export class TopNav {
  private readonly authService = inject(AuthService);
  private readonly dreamTeamState = inject(DreamTeamStateService);
  private readonly router = inject(Router);

  readonly trainer = this.authService.trainer;

  logout(): void {
    this.authService.logout();
    this.dreamTeamState.clear();
    this.router.navigate(['/login']);
  }
}
