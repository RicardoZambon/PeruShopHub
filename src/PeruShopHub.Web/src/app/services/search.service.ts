import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SearchResult } from '../models/api.models';
export type { SearchResult };

@Injectable({ providedIn: 'root' })
export class SearchService {
  private http = inject(HttpClient);

  search(query: string, limit: number = 10): Observable<SearchResult[]> {
    const params = new HttpParams()
      .set('q', query)
      .set('limit', limit);
    return this.http.get<SearchResult[]>('/api/search', { params });
  }
}
