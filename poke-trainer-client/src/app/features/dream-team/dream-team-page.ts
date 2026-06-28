import { Component } from '@angular/core';

@Component({ selector: 'app-dream-team-page', templateUrl: './dream-team-page.html', styleUrl: './dream-team-page.scss' })
export class DreamTeamPage {
  readonly slots = Array.from({ length: 5 }, (_, index) => index + 1);
}
