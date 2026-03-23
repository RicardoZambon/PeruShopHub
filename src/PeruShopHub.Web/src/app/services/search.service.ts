import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SearchResult {
  type: 'pedido' | 'produto' | 'cliente';
  id: string;
  primary: string;
  secondary: string;
  route: string;
}

@Injectable({ providedIn: 'root' })
export class SearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/search`;

  search(query: string, limit?: number): Observable<SearchResult[]> {
    let params = new HttpParams().set('query', query);
    if (limit) params = params.set('limit', limit.toString());
    return this.http.get<SearchResult[]>(this.baseUrl, { params });
  }
}
