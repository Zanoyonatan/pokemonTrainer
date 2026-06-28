import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { SmartSearchResult } from '../../shared/models/smart-search.model';
@Injectable({ providedIn: 'root' })
export class SmartSearchService {
  private readonly http = inject(HttpClient);
  search(query: string): Observable<SmartSearchResult> { return this.http.post<SmartSearchResult>(`${API_BASE_URL}/pokemon/smart-search`, { query }); }
}
