import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { DreamTeamService } from '../dream-team/dream-team.service';
import { TeamAnalysisResult } from '../../shared/models/dream-team.model';
@Injectable({ providedIn: 'root' })
export class TeamAnalyzerService { private readonly dreamTeamService = inject(DreamTeamService); analyze(): Observable<TeamAnalysisResult> { return this.dreamTeamService.analyzeTeam(); } }
