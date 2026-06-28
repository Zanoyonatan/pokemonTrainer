import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { AddToDreamTeamRequest, DreamTeamItem, NicknameSuggestionResult, TeamAnalysisResult, UpdateNicknameRequest } from '../../shared/models/dream-team.model';
@Injectable({ providedIn: 'root' })
export class DreamTeamService {
  private readonly http = inject(HttpClient);
  getTeam(): Observable<DreamTeamItem[]> { return this.http.get<DreamTeamItem[]>(`${API_BASE_URL}/dream-team`); }
  addPokemon(request: AddToDreamTeamRequest): Observable<DreamTeamItem> { return this.http.post<DreamTeamItem>(`${API_BASE_URL}/dream-team`, request); }
  updateNickname(teamItemId: number, nickname: string): Observable<DreamTeamItem> { const request: UpdateNicknameRequest = { nickname }; return this.http.put<DreamTeamItem>(`${API_BASE_URL}/dream-team/${teamItemId}/nickname`, request); }
  removePokemon(teamItemId: number): Observable<void> { return this.http.delete<void>(`${API_BASE_URL}/dream-team/${teamItemId}`); }
  generateNicknames(pokemonId: number): Observable<NicknameSuggestionResult> { return this.http.post<NicknameSuggestionResult>(`${API_BASE_URL}/nicknames/generate`, { pokemonId }); }
  analyzeTeam(): Observable<TeamAnalysisResult> { return this.http.post<TeamAnalysisResult>(`${API_BASE_URL}/dream-team/analyze`, {}); }
}
