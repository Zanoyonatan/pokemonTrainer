import { Injectable, computed, inject, signal } from '@angular/core';
import { finalize, Observable, of, shareReplay, tap } from 'rxjs';

import { DreamTeamService } from './dream-team.service';
import { DreamTeamItem } from '../../shared/models/dream-team.model';

@Injectable({
  providedIn: 'root'
})
export class DreamTeamStateService {
  private readonly dreamTeamService = inject(DreamTeamService);

  private readonly teamSignal = signal<DreamTeamItem[]>([]);
  private loaded = false;
  private inFlightRequest?: Observable<DreamTeamItem[]>;

  readonly team = this.teamSignal.asReadonly();

  readonly teamPokeApiIds = computed(() =>
    new Set(this.teamSignal().map(item => item.pokeApiId))
  );

  load(force = false): Observable<DreamTeamItem[]> {
    if (!force && this.loaded) {
      return of(this.teamSignal());
    }

    if (!force && this.inFlightRequest) {
      return this.inFlightRequest;
    }

    this.inFlightRequest = this.dreamTeamService.getTeam().pipe(
      tap(team => {
        this.teamSignal.set(team);
        this.loaded = true;
      }),
      finalize(() => {
        this.inFlightRequest = undefined;
      }),
      shareReplay(1)
    );

    return this.inFlightRequest;
  }

  refresh(): Observable<DreamTeamItem[]> {
    return this.load(true);
  }

  clear(): void {
    this.teamSignal.set([]);
    this.loaded = false;
    this.inFlightRequest = undefined;
  }
}
