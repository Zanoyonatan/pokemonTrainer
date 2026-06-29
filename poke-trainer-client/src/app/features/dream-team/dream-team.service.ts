import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { API_BASE_URL } from '../../core/config/api.config';
import {
  AddToDreamTeamRequest,
  DreamTeamItem,
  NicknameSuggestionResult,
  TeamAnalysisResult,
  UpdateNicknameRequest
} from '../../shared/models/dream-team.model';

interface TeamBackendObject {
  items?: DreamTeamItem[];
  data?: DreamTeamItem[];
  team?: DreamTeamItem[];
}

type TeamBackendResponse = TeamBackendObject | DreamTeamItem[];

interface NicknameBackendObject {
  pokeApiId?: number;
  suggestions?: string[];
  names?: string[];
}

type NicknameBackendResponse = NicknameBackendObject | string[];

@Injectable({
  providedIn: 'root'
})
export class DreamTeamService {
  private readonly http = inject(HttpClient);

  getTeam(): Observable<DreamTeamItem[]> {
    return this.http
      .get<TeamBackendResponse>(`${API_BASE_URL}/dream-team`)
      .pipe(map(response => normalizeTeamResponse(response)));
  }

  addPokemon(request: AddToDreamTeamRequest): Observable<DreamTeamItem> {
    return this.http.post<DreamTeamItem>(`${API_BASE_URL}/dream-team`, request);
  }

  updateNickname(teamItemId: number, nickname: string): Observable<DreamTeamItem> {
  const request: UpdateNicknameRequest = { nickname };

  return this.http.put<DreamTeamItem>(
    `${API_BASE_URL}/dream-team/${teamItemId}`,
    request
  );
}

  removePokemon(teamItemId: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/dream-team/${teamItemId}`);
  }

  generateNicknames(pokeApiId: number, count = 5): Observable<NicknameSuggestionResult> {
    return this.http
      .post<NicknameBackendResponse>(`${API_BASE_URL}/pokemon/${pokeApiId}/generate-nicknames`, { count })
      .pipe(map(response => normalizeNicknameResponse(pokeApiId, response)));
  }

  analyzeTeam(): Observable<TeamAnalysisResult> {
    return this.http.get<TeamAnalysisResult>(`${API_BASE_URL}/dream-team/analyze`);
  }
}

function normalizeTeamResponse(response: TeamBackendResponse): DreamTeamItem[] {
  if (Array.isArray(response)) {
    return response;
  }

  return response.items ?? response.data ?? response.team ?? [];
}

function normalizeNicknameResponse(
  pokeApiId: number,
  response: NicknameBackendResponse
): NicknameSuggestionResult {
  if (Array.isArray(response)) {
    return {
      pokeApiId,
      suggestions: response
    };
  }

  return {
    pokeApiId: response.pokeApiId ?? pokeApiId,
    suggestions: response.suggestions ?? response.names ?? []
  };
}
